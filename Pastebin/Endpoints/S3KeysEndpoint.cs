using Microsoft.AspNetCore.Mvc;
using Pastebin.Database;
using Pastebin.Interfaces;

namespace Pastebin.Endpoints;

public static class S3KeysEndpoint
{
    public static readonly List<S3Key> DeletionList = [];

    public static void MapS3Keys(this RouteGroupBuilder app)
    {
        app.MapPost("/", async (
            [FromServices] IKeysRepository keysRepository,
            [FromServices] ITextRepository textRepository,
            [FromBody] string text,
            [FromQuery] DateTime? expirationDateTime) =>
        {
            var s3Key = expirationDateTime is not null
                ? new S3Key { Key = Guid.NewGuid().ToString(), ExpirationDateTime = expirationDateTime }
                : new S3Key { Key = Guid.NewGuid().ToString() };

            await keysRepository.PostAsync(s3Key);

            await textRepository.PostAsync(s3Key.Key!, text);

            if (expirationDateTime is not null)
                DeletionList.Add(s3Key);

            var hash = Convert.ToBase64String(s3Key.Id.ToByteArray())
                .Replace("/", "-")
                .Replace("+", "_")
                .Replace("=", "");

            return $"http://localhost:8080/{hash}";
        });

        app.MapGet("/{hash}", async (
            [FromServices] IKeysRepository keysRepository,
            [FromServices] ITextRepository textRepository,
            [FromRoute] string hash) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            var s3Key = await keysRepository.GetByIdAsync(guid);
            if (s3Key is null)
                return Results.NotFound("S3 key wasn't found");

            var s3Object = await textRepository.GetByKeyAsync(s3Key.Key!);

            return s3Object is null ? Results.NotFound("S3 object wasn't found") : Results.Ok(s3Object);
        });

        app.MapPut("/{hash}", async (
            [FromServices] IKeysRepository keysRepository,
            [FromServices] ITextRepository textRepository,
            [FromRoute] string hash,
            [FromBody] string? text,
            [FromQuery] DateTime? expirationDateTime) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            if (expirationDateTime is not null)
            {
                var s3Key = await keysRepository.EditByIdAsync(guid, expirationDateTime);
                if (s3Key is null)
                    return Results.NotFound("S3 key wasn't found");

                if (!DeletionList.Contains(s3Key))
                    DeletionList.Add(s3Key);
            }

            if (text is not null)
            {
                var s3Key = await keysRepository.GetByIdAsync(guid);
                if (s3Key is null)
                    return Results.NotFound("S3 key wasn't found");
                await textRepository.EditByKeyAsync(s3Key.Key!, text);
            }

            return Results.Ok();
        });

        app.MapDelete("/{hash}", async (
            [FromServices] IKeysRepository keysRepository,
            [FromServices] ITextRepository textRepository,
            [FromRoute] string hash) =>
        {
            var guid = new Guid(Convert.FromBase64String(hash
                .Replace("-", "/")
                .Replace("_", "+") + "=="));

            var s3Key = await keysRepository.DeleteByIdAsync(guid);
            if (s3Key is null)
                return Results.NotFound("S3 key wasn't found");

            await textRepository.DeleteByKeyAsync(s3Key.Key!);

            DeletionList.Remove(s3Key);

            return Results.Ok();
        });
    }
}