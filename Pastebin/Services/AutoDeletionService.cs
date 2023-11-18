﻿using Amazon.S3;
using Amazon.S3.Model;
using NCrontab;
using Pastebin.Database;
using Pastebin.Endpoints;

namespace Pastebin.Services;

public class AutoDeletionService : BackgroundService
{
    private readonly CrontabSchedule _schedule;

    private readonly IServiceScopeFactory _scopeFactory;

    private DateTime _nextRun;

    public AutoDeletionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
    }

    private static string Schedule => "1 * * * * *"; //Runs every 1 minute

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var now = DateTime.Now;
            if (now > _nextRun)
            {
                Process();
                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
            }

            await Task.Delay(5000, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    private async void Process()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd hh:mm");
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();
        var amazonS3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var deleteObjectsRequest = new DeleteObjectsRequest
            { BucketName = configuration.GetSection("BucketName").Value };

        if (S3KeysEndpoint.DeletionQueue.Count == 0)
            return;
        
        while (S3KeysEndpoint.DeletionQueue.Count > 0)
        {
            var s3Key = S3KeysEndpoint.DeletionQueue.Peek();
            if (s3Key.ExpirationDateTime.ToString("yyyy-MM-dd hh:mm") != now)
                break;
            deleteObjectsRequest.AddKey(s3Key.Key);
            dbContext.Keys.Remove(s3Key);
            S3KeysEndpoint.DeletionQueue.Dequeue();
        }

        if (deleteObjectsRequest.Objects.Count != 0) await amazonS3Client.DeleteObjectsAsync(deleteObjectsRequest);
        await dbContext.SaveChangesAsync();
    }
}