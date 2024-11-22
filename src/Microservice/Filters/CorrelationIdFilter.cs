using System;
using AGTec.Common.Base.Accessors;
using Correlate;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AGTec.Microservice.Filters;

public class CorrelationIdFilter : IActionFilter
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationIdFilter(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var correlationId = _correlationContextAccessor.CorrelationContext.CorrelationId;
        CorrelationIdAccessor.CorrelationId = Guid.Parse(correlationId);
    }
}