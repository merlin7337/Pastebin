using Amazon.S3;
using Amazon.S3.Model;
using Pastebin.Interfaces;
using StackExchange.Redis;

namespace Pastebin.Database.CachedRepositories;

public class CachedTextRepository(
    IAmazonS3 amazonS3Client,
    IConnectionMultiplexer connection,
    IConfiguration configuration) : ITextRepository
{
    private const string Prefix = "text";
    private readonly IDatabase _redis = connection.GetDatabase();

    public async Task PostAsync(string key, string text)
    {
        var putObjectRequest = new PutObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName"),
            ContentBody = text
        };
        putObjectRequest.Metadata.Add("Content-Type", "text/plain");
        await amazonS3Client.PutObjectAsync(putObjectRequest);
    }

    public async Task<string?> GetByKeyAsync(string key)
    {
        var text = await _redis.StringGetAsync($"{Prefix}:{key}");
        if (text.HasValue)
            return text;

        var getObjectRequest = new GetObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName")
        };

        var s3Object = await amazonS3Client.GetObjectAsync(getObjectRequest);
        if (s3Object is null)
            return null;

        using var sr = new StreamReader(s3Object.ResponseStream);
        text = await sr.ReadToEndAsync();

        await _redis.StringSetAsync($"{Prefix}:{key}", text, TimeSpan.FromMinutes(30));

        return text;
    }

    public async Task EditByKeyAsync(string key, string text)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName")
        };
        await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

        var putObjectRequest = new PutObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName"),
            ContentBody = text
        };
        putObjectRequest.Metadata.Add("Content-Type", "text/plain");
        await amazonS3Client.PutObjectAsync(putObjectRequest);

        await _redis.KeyDeleteAsync($"{Prefix}:{key}");
        await _redis.StringSetAsync($"{Prefix}:{key}", text, TimeSpan.FromMinutes(30));
    }

    public async Task DeleteByKeyAsync(string key)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName")
        };
        await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

        await _redis.KeyDeleteAsync($"{Prefix}:{key}");
    }

    public async Task DeleteMultipleByKeysListAsync(List<string> keys)
    {
        var deleteObjectsRequest = new DeleteObjectsRequest
            { BucketName = configuration.GetValue<string>("BucketName") };

        foreach (var key in keys)
        {
            deleteObjectsRequest.AddKey(key);
            await _redis.KeyDeleteAsync($"{Prefix}:{key}");
        }

        if (deleteObjectsRequest.Objects.Count > 0)
            await amazonS3Client.DeleteObjectsAsync(deleteObjectsRequest);
    }
}