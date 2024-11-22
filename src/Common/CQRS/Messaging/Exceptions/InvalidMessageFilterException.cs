using System;

namespace AGTec.Common.CQRS.Messaging.Exceptions;

public class InvalidMessageFilterException : Exception
{
    public InvalidMessageFilterException()
    {
    }

    public InvalidMessageFilterException(string message) : base(message)
    {
    }
}