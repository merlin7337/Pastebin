using System.Text.Json;
using Pastebin.Interfaces;
using StackExchange.Redis;

namespace Pastebin.Database.CachedRepositories;

public class CachedKeysRepository(
    ApplicationDbContext postgreSql,
    IConnectionMultiplexer connection) : IKeysRepository
{
    private const string Prefix = "metadata";
    private readonly IDatabase _redis = connection.GetDatabase();

    public async Task PostAsync(S3Key s3Key)
    {
        postgreSql.Keys.Add(s3Key);
        await postgreSql.SaveChangesAsync();
    }

    public async Task<S3Key?> GetByIdAsync(Guid id)
    {
        var s3KeyString = await _redis.StringGetAsync($"{Prefix}:{id.ToString()}");
        if (s3KeyString.HasValue)
            return JsonSerializer.Deserialize<S3Key>(s3KeyString!);

        var s3Key = await postgreSql.Keys.FindAsync(id);
        if (s3Key is null)
            return null;

        s3KeyString = JsonSerializer.Serialize(s3Key);
        await _redis.StringSetAsync($"{Prefix}:{id.ToString()}", s3KeyString, TimeSpan.FromMinutes(30));

        return s3Key;
    }

    public async Task<S3Key?> EditByIdAsync(Guid id, DateTime? expirationDateTime)
    {
        var s3Key = await postgreSql.Keys.FindAsync(id);
        if (s3Key is null)
            return null;

        s3Key.ExpirationDateTime = expirationDateTime;

        postgreSql.Keys.Update(s3Key);
        await postgreSql.SaveChangesAsync();

        var s3KeyString = JsonSerializer.Serialize(s3Key);

        await _redis.KeyDeleteAsync(id.ToString());
        await _redis.StringSetAsync($"{Prefix}:{id.ToString()}", s3KeyString, TimeSpan.FromMinutes(30));

        return s3Key;
    }

    public async Task<S3Key?> DeleteByIdAsync(Guid id)
    {
        var s3Key = await postgreSql.Keys.FindAsync(id);
        if (s3Key is null)
            return null;

        postgreSql.Keys.Remove(s3Key);
        await postgreSql.SaveChangesAsync();

        await _redis.KeyDeleteAsync($"{Prefix}:{id.ToString()}");

        return s3Key;
    }
}