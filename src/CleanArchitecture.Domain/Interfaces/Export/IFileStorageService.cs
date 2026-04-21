using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Interfaces.Export;

/// <summary>
/// Interface for file storage operations in MinIO.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload file to MinIO.
    /// </summary>
    /// <param name="bucketName">MinIO bucket name</param>
    /// <param name="objectName">Object name in bucket</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL to access the uploaded file</returns>
    Task<string> UploadFileAsync(
        string bucketName,
        string objectName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file from MinIO.
    /// </summary>
    /// <param name="bucketName">MinIO bucket name</param>
    /// <param name="objectName">Object name in bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if bucket exists, create if not.
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file size from MinIO.
    /// </summary>
    /// <param name="bucketName">MinIO bucket name</param>
    /// <param name="objectName">Object name in bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File size in bytes</returns>
    Task<long> GetFileSizeAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(
    string bucket,
    string objectName,
    int expiresInSeconds = 3600,
    CancellationToken cancellationToken = default);
}
