namespace AGTec.Platform;

/// <summary>
/// Issues short-lived, signed URLs so clients upload/download objects <b>directly</b> to the
/// storage provider, bypassing the application (Azure Blob SAS, S3 pre-signed URLs, …).
/// A separate port from <see cref="IObjectStore"/> because not every store supports it, and
/// because direct upload means the app never sees the bytes — pair it with scanning at the
/// storage layer (e.g. Defender for Storage) rather than the inline <see cref="IMalwareScanner"/>.
/// </summary>
public interface IObjectUrlProvider
{
    /// <summary>A time-limited URL the client can PUT the object to directly.</summary>
    Task<Uri> CreateUploadUrlAsync(
        string container,
        string key,
        TimeSpan validFor,
        string? contentType = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>A time-limited URL the client can GET the object from directly.</summary>
    Task<Uri> CreateDownloadUrlAsync(
        string container,
        string key,
        TimeSpan validFor,
        CancellationToken cancellationToken = default
    );
}
