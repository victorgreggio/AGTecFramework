using System;

namespace AGTec.Common.CQRS.Exceptions;

public class MessageTypeNotSupportedException : Exception
{
    public MessageTypeNotSupportedException()
    {
    }

    public MessageTypeNotSupportedException(string message) : base(message)
    {
    }
}