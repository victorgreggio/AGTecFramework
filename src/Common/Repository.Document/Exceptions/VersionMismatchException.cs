using System;

namespace AGTec.Common.Repository.Document.Exceptions;

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