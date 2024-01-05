using Pastebin.Interfaces;

namespace Pastebin.Database.DefaultRepositories;

public class PostgreSqlKeysRepository(ApplicationDbContext postgreSql) : IKeysRepository
{
    public async Task PostAsync(S3Key s3Key)
    {
        postgreSql.Keys.Add(s3Key);
        await postgreSql.SaveChangesAsync();
    }

    public async Task<S3Key?> GetByIdAsync(Guid id)
    {
        var s3Key = await postgreSql.Keys.FindAsync(id);
        return s3Key ?? null;
    }

    public async Task<S3Key?> EditByIdAsync(Guid id, DateTime? expirationDateTime)
    {
        var s3Key = await postgreSql.Keys.FindAsync(id);
        if (s3Key is null)
            return null;

        s3Key.ExpirationDateTime = expirationDateTime;

        postgreSql.Keys.Update(s3Key);
        await postgreSql.SaveChangesAsync();

        return s3Key;
    }

    public async Task<S3Key?> DeleteByIdAsync(Guid id)
    {
        var s3Key = await postgreSql.Keys.FindAsync(id);
        if (s3Key is null)
            return null;

        postgreSql.Keys.Remove(s3Key);
        await postgreSql.SaveChangesAsync();

        return s3Key;
    }
}