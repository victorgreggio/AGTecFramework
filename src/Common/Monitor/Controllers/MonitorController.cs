using AGTec.Common.BackgroundTaskQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.Monitor.Controllers;

[AllowAnonymous]
[Route("[controller]")]
public class MonitorController : Controller
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<MonitorController> _logger;

    public MonitorController(IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<MonitorController> logger)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    [Route("profiler")]
    public IActionResult MiniProfiler()
    {
        return View();
    }

    [Route("queue")]
    public IActionResult BackgroundQueue()
    {
        var queuedTasks = _backgroundTaskQueue.QueuedTasks;
        return View(queuedTasks);
    }

    [Route("queue/pause")]
    public IActionResult PauseQueue()
    {
        _backgroundTaskQueue.Pause();
        return Content("Queue stopped.");
    }

    [Route("queue/restart")]
    public IActionResult RestartQueue()
    {
        _backgroundTaskQueue.Restart();
        return Content("Queue restarted.");
    }
}