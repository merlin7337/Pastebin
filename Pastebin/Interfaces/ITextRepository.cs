namespace Pastebin.Interfaces;

public interface ITextRepository
{
    Task PostAsync(string key, string text);
    Task<string?> GetByKeyAsync(string key);
    Task EditByKeyAsync(string key, string text);
    Task DeleteByKeyAsync(string key);
    Task DeleteMultipleByKeysListAsync(List<string> keys);
}