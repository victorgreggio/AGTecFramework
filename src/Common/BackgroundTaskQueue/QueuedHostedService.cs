using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.BackgroundTaskQueue;

public class QueuedHostedService : BackgroundService
{
    private const int TASK_DELAY = 5000; // 5 seconds
    private readonly ILogger<QueuedHostedService> _logger;

    private readonly IBackgroundTaskQueue _tasksToRun;

    public QueuedHostedService(IBackgroundTaskQueue tasksToRun,
        ILogger<QueuedHostedService> logger)
    {
        _tasksToRun = tasksToRun;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            while (!_tasksToRun.Paused && stoppingToken.IsCancellationRequested == false)
            {
                var taskToRun = await _tasksToRun.Dequeue(stoppingToken);

                try
                {
                    _logger.LogInformation($"Starting '{taskToRun.Key}'.");

                    await taskToRun.Value(stoppingToken);

                    _logger.LogInformation($"Finished '{taskToRun.Key}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error running task {nameof(taskToRun.Key)}.");
                }
            }

            await Task.Delay(TASK_DELAY, stoppingToken);
        }
    }
}