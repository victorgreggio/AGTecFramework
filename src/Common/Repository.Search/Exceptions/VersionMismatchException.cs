using System;

namespace AGTec.Common.Repository.Search.Exceptions;

public class VersionMismatchException : Exception
{
    public VersionMismatchException()
    {
    }

    public VersionMismatchException(string message) : base(message)
    {
    }

    public VersionMismatchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}