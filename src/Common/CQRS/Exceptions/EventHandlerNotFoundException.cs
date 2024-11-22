using System;

namespace AGTec.Common.CQRS.Exceptions;

public class EventHandlerNotFoundException : Exception
{
    public EventHandlerNotFoundException()
    {
    }

    public EventHandlerNotFoundException(string message) : base(message)
    {
    }
}