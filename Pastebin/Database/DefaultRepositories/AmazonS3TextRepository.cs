using Amazon.S3;
using Amazon.S3.Model;
using Pastebin.Interfaces;

namespace Pastebin.Database.DefaultRepositories;

public class AmazonS3TextRepository(IAmazonS3 amazonS3Client, IConfiguration configuration) : ITextRepository
{
    public async Task PostAsync(string key, string text)
    {
        var putObjectRequest = new PutObjectRequest
        {
            Key = key,
            BucketName = configuration.GetSection("BucketName").Value,
            ContentBody = text
        };
        putObjectRequest.Metadata.Add("Content-Type", "text/plain");
        await amazonS3Client.PutObjectAsync(putObjectRequest);
    }

    public async Task<string?> GetByKeyAsync(string key)
    {
        var getObjectRequest = new GetObjectRequest
        {
            Key = key,
            BucketName = configuration.GetSection("BucketName").Value
        };
        var s3Object = await amazonS3Client.GetObjectAsync(getObjectRequest);

        using var sr = new StreamReader(s3Object.ResponseStream);
        var text = await sr.ReadToEndAsync();

        return text;
    }

    public async Task EditByKeyAsync(string key, string text)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            Key = key,
            BucketName = configuration.GetSection("BucketName").Value
        };
        await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);

        var putObjectRequest = new PutObjectRequest
        {
            Key = key,
            BucketName = configuration.GetSection("BucketName").Value,
            ContentBody = text
        };
        putObjectRequest.Metadata.Add("Content-Type", "text/plain");
        await amazonS3Client.PutObjectAsync(putObjectRequest);
    }

    public async Task DeleteByKeyAsync(string key)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            Key = key,
            BucketName = configuration.GetSection("BucketName").Value
        };
        await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);
    }

    public async Task DeleteMultipleByKeysListAsync(List<string> keys)
    {
        var deleteObjectsRequest = new DeleteObjectsRequest
            { BucketName = configuration.GetSection("BucketName").Value };

        foreach (var key in keys) deleteObjectsRequest.AddKey(key);

        await amazonS3Client.DeleteObjectsAsync(deleteObjectsRequest);
    }
}