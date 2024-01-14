using Microsoft.EntityFrameworkCore;

namespace Pastebin.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<S3Key> Keys { get; init; } = null!;
}