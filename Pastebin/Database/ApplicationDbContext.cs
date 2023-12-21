using Microsoft.EntityFrameworkCore;

namespace Pastebin.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<S3Key> Keys { get; init; } = null!;
}