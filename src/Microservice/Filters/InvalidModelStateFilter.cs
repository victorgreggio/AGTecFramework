using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace AGTec.Microservice.Filters;

public class InvalidModelStateFilter : IActionFilter
{
    private readonly ILogger<InvalidModelStateFilter> _logger;

    public InvalidModelStateFilter(ILogger<InvalidModelStateFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid == false)
        {
            _logger.LogWarning(string.Join(Environment.NewLine,
                context.ModelState.Values.SelectMany(state => state.Errors).Select(error => error.ErrorMessage)));

            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }
}