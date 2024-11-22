using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AGTec.Common.BackgroundTaskQueue;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly SemaphoreSlim _signal;
    private readonly ConcurrentQueue<KeyValuePair<string, Func<CancellationToken, Task>>> _workItems;

    public BackgroundTaskQueue()
    {
        _workItems = new ConcurrentQueue<KeyValuePair<string, Func<CancellationToken, Task>>>();
        _signal = new SemaphoreSlim(0);
    }

    public void Queue(string workName, Func<CancellationToken, Task> workItem)
    {
        if (string.IsNullOrWhiteSpace(workName)) throw new ArgumentNullException(nameof(workName));

        if (workItem == null) throw new ArgumentNullException(nameof(workItem));

        _workItems.Enqueue(new KeyValuePair<string, Func<CancellationToken, Task>>(workName, workItem));
        _signal.Release();
    }

    public async Task<KeyValuePair<string, Func<CancellationToken, Task>>> Dequeue(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out var workItem);

        return workItem;
    }

    public void Pause()
    {
        Paused = true;
    }

    public void Restart()
    {
        Paused = false;
    }

    public IEnumerable<string> QueuedTasks => _workItems.Select(x => x.Key);
    public bool Paused { get; private set; }
}