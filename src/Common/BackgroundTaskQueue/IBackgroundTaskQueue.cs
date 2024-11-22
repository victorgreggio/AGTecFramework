using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AGTec.Common.BackgroundTaskQueue;

public interface IBackgroundTaskQueue
{
    IEnumerable<string> QueuedTasks { get; }

    bool Paused { get; }
    void Queue(string workName, Func<CancellationToken, Task> workItem);

    Task<KeyValuePair<string, Func<CancellationToken, Task>>> Dequeue(CancellationToken cancellationToken);
    void Pause();
    void Restart();
}