using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AGTec.Common.BackgroundTaskQueue;

public abstract class BackgroundService<T> : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _stoppingCts = new();

    public virtual void Dispose()
    {
        _stoppingCts.Cancel();
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(_stoppingCts.Token);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() => _stoppingCts.Cancel(), cancellationToken);
    }

    protected abstract Task<T> ExecuteAsync(CancellationToken stoppingToken);
}