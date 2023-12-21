using Amazon.S3;
using Amazon.S3.Model;
using Pastebin.Database;

namespace Pastebin.Endpoints;

public static class S3KeysEndpoint
{
    public static readonly List<S3Key> DeletionList = new();

    public static void MapS3Keys(this RouteGroupBuilder app)
    {
        app.MapGet("/{hash}", async (ApplicationDbContext dbContext, IAmazonS3 amazonS3Client,
            IConfiguration configuration, string hash) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            var s3Key = await dbContext.Keys.FindAsync(guid);
            if (s3Key == null)
                return Results.NotFound();

            var getObjectRequest = new GetObjectRequest
            {
                Key = s3Key.Key,
                BucketName = configuration.GetSection("BucketName").Value
            };
            var s3Object = await amazonS3Client.GetObjectAsync(getObjectRequest);

            using var sr = new StreamReader(s3Object.ResponseStream);
            return Results.Ok(await sr.ReadToEndAsync());
        });

        app.MapPost("/", async (ApplicationDbContext dbContext, IAmazonS3 amazonS3Client,
            IConfiguration configuration, string text, DateTime? expirationDateTime) =>
        {
            var s3Key = expirationDateTime != null
                ? new S3Key { Key = Guid.NewGuid().ToString(), ExpirationDateTime = expirationDateTime }
                : new S3Key { Key = Guid.NewGuid().ToString() };
            if (expirationDateTime != null)
                DeletionList.Add(s3Key);

            var putObjectRequest = new PutObjectRequest
            {
                Key = s3Key.Key,
                BucketName = configuration.GetSection("BucketName").Value,
                ContentBody = text
            };
            putObjectRequest.Metadata.Add("Content-Type", "text/plain");
            await amazonS3Client.PutObjectAsync(putObjectRequest);

            dbContext.Keys.Add(s3Key);
            await dbContext.SaveChangesAsync();

            var hash = Convert.ToBase64String(s3Key.Id.ToByteArray())
                .Replace("/", "-")
                .Replace("+", "_")
                .Replace("=", "");

            return $"https://localhost:7053/{hash}";
        });

        app.MapPut("/{hash}", async (ApplicationDbContext dbContext, IAmazonS3 amazonS3Client,
            IConfiguration configuration, string hash, string? text, DateTime? expirationDateTime) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            var s3Key = await dbContext.Keys.FindAsync(guid);
            if (s3Key == null)
                return Results.NotFound();

            if (expirationDateTime != null)
            {
                s3Key.ExpirationDateTime = expirationDateTime;
                dbContext.Keys.Update(s3Key);
                await dbContext.SaveChangesAsync();
                if (!DeletionList.Contains(s3Key))
                    DeletionList.Add(s3Key);
            }

            if (text == null)
                return Results.Ok();

            var deleteObjectRequest = new DeleteObjectRequest
            {
                Key = s3Key.Key,
                BucketName = configuration.GetSection("BucketName").Value
            };
            await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

            var putObjectRequest = new PutObjectRequest
            {
                Key = s3Key.Key,
                BucketName = configuration.GetSection("BucketName").Value,
                ContentBody = text
            };
            putObjectRequest.Metadata.Add("Content-Type", "text/plain");
            await amazonS3Client.PutObjectAsync(putObjectRequest);

            return Results.Ok();
        });

        app.MapDelete("/{hash}", async (ApplicationDbContext dbContext, IAmazonS3 amazonS3Client,
            IConfiguration configuration, string hash) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            var s3Key = await dbContext.Keys.FindAsync(guid);
            if (s3Key == null)
                return Results.NotFound();

            var deleteObjectRequest = new DeleteObjectRequest
            {
                Key = s3Key.Key,
                BucketName = configuration.GetSection("BucketName").Value
            };
            await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

            dbContext.Keys.Remove(s3Key);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
    }
}