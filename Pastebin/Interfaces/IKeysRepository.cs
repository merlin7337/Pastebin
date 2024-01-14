using Pastebin.Database;

namespace Pastebin.Interfaces;

public interface IKeysRepository
{
    Task PostAsync(S3Key s3Key);
    Task<S3Key?> GetByIdAsync(Guid id);
    Task<S3Key?> EditByIdAsync(Guid id, DateTime? expirationDateTime);
    Task<S3Key?> DeleteByIdAsync(Guid id);
}