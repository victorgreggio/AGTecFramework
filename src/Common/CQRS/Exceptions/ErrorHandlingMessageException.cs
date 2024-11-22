using System;

namespace AGTec.Common.CQRS.Exceptions;

public class ErrorHandlingMessageException : Exception
{
    public ErrorHandlingMessageException()
    {
    }

    public ErrorHandlingMessageException(string message) : base(message)
    {
    }

    public ErrorHandlingMessageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}