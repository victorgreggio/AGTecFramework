using System;

namespace AGTec.Common.CQRS.Exceptions;

public class PayloadSerializerNotFound : Exception
{
    public PayloadSerializerNotFound()
    {
    }

    public PayloadSerializerNotFound(string message) : base(message)
    {
    }
}