using System;

namespace AGTec.Common.CQRS.Exceptions;

public class QueryHandlerNotFoundException : Exception
{
    public QueryHandlerNotFoundException()
    {
    }

    public QueryHandlerNotFoundException(string message) : base(message)
    {
    }
}