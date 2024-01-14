using Amazon.S3;
using Amazon.S3.Model;
using Pastebin.Interfaces;

namespace Pastebin.Database.DefaultRepositories;

public class AmazonS3TextRepository(
    IAmazonS3 amazonS3Client,
    IConfiguration configuration) : ITextRepository
{
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
        var getObjectRequest = new GetObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName")
        };

        var s3Object = await amazonS3Client.GetObjectAsync(getObjectRequest);
        if (s3Object is null)
            return null;

        using var sr = new StreamReader(s3Object.ResponseStream);
        var text = await sr.ReadToEndAsync();

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
    }

    public async Task DeleteByKeyAsync(string key)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            Key = key,
            BucketName = configuration.GetValue<string>("BucketName")
        };
        await amazonS3Client.DeleteObjectAsync(deleteObjectRequest);
    }

    public async Task DeleteMultipleByKeysListAsync(List<string> keys)
    {
        var deleteObjectsRequest = new DeleteObjectsRequest
            { BucketName = configuration.GetValue<string>("BucketName") };

        foreach (var key in keys)
            deleteObjectsRequest.AddKey(key);

        if (deleteObjectsRequest.Objects.Count > 0)
            await amazonS3Client.DeleteObjectsAsync(deleteObjectsRequest);
    }
}