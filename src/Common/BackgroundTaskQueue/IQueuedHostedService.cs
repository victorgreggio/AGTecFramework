using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AGTec.Common.BackgroundTaskQueue;

public interface IQueuedHostedService : IHostedService
{
    Task Stop();
    Task Restart();
}