using NCrontab;
using Pastebin.Endpoints;
using Pastebin.Interfaces;

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
        if (S3KeysEndpoint.DeletionList.Count == 0)
            return;

        var now = DateTime.Now.ToString("yyyy-MM-dd hh:mm");

        using var scope = _scopeFactory.CreateScope();
        var keysRepository = scope.ServiceProvider.GetRequiredService<IKeysRepository>();
        var textRepository = scope.ServiceProvider.GetRequiredService<ITextRepository>();

        S3KeysEndpoint.DeletionList.Sort(
            (x, y) => x.ExpirationDateTime!.Value.CompareTo(y.ExpirationDateTime!.Value));

        var keysToDelete = new List<string>();

        while (S3KeysEndpoint.DeletionList.Count > 0)
        {
            var s3Key = S3KeysEndpoint.DeletionList[0];

            if (s3Key.ExpirationDateTime?.ToString("yyyy-MM-dd hh:mm") != now)
                break;

            keysToDelete.Add(s3Key.Key!);

            await keysRepository.DeleteByIdAsync(s3Key.Id);

            S3KeysEndpoint.DeletionList.Remove(s3Key);
        }

        await textRepository.DeleteMultipleByKeysListAsync(keysToDelete);
    }
}