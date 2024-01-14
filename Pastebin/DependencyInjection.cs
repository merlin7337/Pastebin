using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pastebin.Database;
using Pastebin.Database.CachedRepositories;
using Pastebin.Database.DefaultRepositories;
using Pastebin.Interfaces;
using Pastebin.Services;
using StackExchange.Redis;

namespace Pastebin;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetValue<string>("ConnectionStrings:PostgreSql")));

        if (configuration.GetValue<bool>("Tools:CacheDbRequests"))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(configuration.GetValue<string>("ConnectionStrings:Redis")!));
            services.AddScoped<IKeysRepository, CachedKeysRepository>();
        }
        else
        {
            services.AddScoped<IKeysRepository, PostgreSqlKeysRepository>();
        }

        var awsOptions = configuration.GetAWSOptions();
        awsOptions.Credentials = new BasicAWSCredentials(
            configuration.GetValue<string>("AWS:AccessKey"),
            configuration.GetValue<string>("AWS:SecretKey"));
        services.AddDefaultAWSOptions(awsOptions);
        services.AddAWSService<IAmazonS3>();

        if (configuration.GetValue<bool>("Tools:CacheObjectStorageRequests"))
        {
            services.TryAddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(configuration.GetValue<string>("ConnectionStrings:Redis")!));
            services.AddScoped<ITextRepository, CachedTextRepository>();
        }
        else
        {
            services.AddScoped<ITextRepository, AmazonS3TextRepository>();
        }

        services.AddHostedService<AutoDeletionService>();
    }
}