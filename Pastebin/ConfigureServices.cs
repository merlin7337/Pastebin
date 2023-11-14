using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Pastebin.Database;

namespace Pastebin;

public static class ConfigureServices
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddDbContext<PostgreSqlDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonS3>();

        services.AddHostedService<AutoDeletionService>();
    }
}