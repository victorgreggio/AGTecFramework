using System;

namespace AGTec.Common.CQRS.Exceptions;

public class SerializerContentTypeMismatch : Exception
{
    public SerializerContentTypeMismatch()
    {
    }

    public SerializerContentTypeMismatch(string message) : base(message)
    {
    }
}