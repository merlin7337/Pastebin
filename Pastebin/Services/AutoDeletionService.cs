using NCrontab;
using Pastebin.Endpoints;
using Pastebin.Interfaces;

namespace Pastebin.Services;

public class AutoDeletionService : BackgroundService
{
    private const string DateTimeStringFormat = "yyyy-MM-dd hh:mm";
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

        var now = DateTime.Now.ToString(DateTimeStringFormat);

        using var scope = _scopeFactory.CreateScope();
        var keysRepository = scope.ServiceProvider.GetRequiredService<IKeysRepository>();
        var textRepository = scope.ServiceProvider.GetRequiredService<ITextRepository>();

        var s3KeysToDelete = new List<string>();

        var validDateDeletionList =
            S3KeysEndpoint.DeletionList.Where(x => x.ExpirationDateTime?.ToString(DateTimeStringFormat) == now)
                .ToList();
        S3KeysEndpoint.DeletionList.RemoveAll(x => x.ExpirationDateTime?.ToString(DateTimeStringFormat) == now);

        foreach (var s3Key in validDateDeletionList)
        {
            var test = await keysRepository.DeleteByIdAsync(s3Key.Id);
            Console.WriteLine(test!.ExpirationDateTime);
            s3KeysToDelete.Add(s3Key.Key!);
        }

        await textRepository.DeleteMultipleByKeysListAsync(s3KeysToDelete);
    }
}