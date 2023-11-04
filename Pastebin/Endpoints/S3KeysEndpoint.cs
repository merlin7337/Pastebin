using Amazon.S3;
using Amazon.S3.Model;
using Pastebin.Database;

namespace Pastebin.Endpoints;

public static class S3KeysEndpoint
{
    public static void MapS3Keys(this RouteGroupBuilder app)
    {
        app.MapGet("/{hash}",
            async (PostgreSqlDbContext db, IAmazonS3 amazonS3Client, IConfiguration configuration, string hash) =>
            {
                var guid = new Guid(Convert.FromBase64String(hash
                    .Replace("-", "/")
                    .Replace("_", "+") + "=="));
                var s3Key = await db.Keys.FindAsync(guid);
                var s3Object =
                    await amazonS3Client.GetObjectAsync(configuration.GetSection("BucketName").Value, s3Key?.Key);
                using var sr = new StreamReader(s3Object.ResponseStream);
                return Results.Ok(await sr.ReadToEndAsync());
            });

        app.MapPost("/",
            async (PostgreSqlDbContext db, IAmazonS3 amazonS3Client, IConfiguration configuration, string text) =>
            {
                var s3Key = new S3Key { Key = Guid.NewGuid().ToString() };

                var request = new PutObjectRequest
                {
                    BucketName = configuration.GetSection("BucketName").Value,
                    Key = s3Key.Key,
                    ContentBody = text
                };
                request.Metadata.Add("Content-Type", "text/plain");
                await amazonS3Client.PutObjectAsync(request);

                db.Keys.Add(s3Key);
                await db.SaveChangesAsync();
                var hash = Convert.ToBase64String(s3Key.Id.ToByteArray())
                    .Replace("/", "-")
                    .Replace("+", "_")
                    .Replace("=", "");
                return $"https://localhost:7053/{hash}";
            });
    }
}