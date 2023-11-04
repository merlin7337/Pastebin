using Microsoft.EntityFrameworkCore;

namespace Pastebin.Database;

public class PostgreSqlDbContext : DbContext
{
    public PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : base(options)
    {
    }

    public DbSet<S3Key> Keys { get; set; } = null!;
}