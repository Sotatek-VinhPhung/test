using CleanArchitecture.Domain.Interfaces.Export;
using Minio;
using Minio.DataModel.Args;

namespace CleanArchitecture.Infrastructure.FileStorage;

/// <summary>
/// MinIO implementation of IFileStorageService.
/// Handles file upload, delete, and retrieval operations.
/// </summary>
public class MinIOFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _baseUrl;

    public MinIOFileStorageService(IMinioClient minioClient, string baseUrl)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    public async Task<string> UploadFileAsync(
        string bucketName,
        string objectName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(objectName);
        ArgumentNullException.ThrowIfNull(fileStream);

        try
        {
            // Reset stream position
            fileStream.Position = 0;

            // Save stream to temporary file
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
            try
            {
                using (var fileHandle = File.Create(tempPath))
                {
                    await fileStream.CopyToAsync(fileHandle, cancellationToken);
                }

                // Get file info
                var fileInfo = new FileInfo(tempPath);

                // Upload file using PutObjectAsync with file path
                using (var uploadStream = File.OpenRead(tempPath))
                {
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithObjectSize(fileInfo.Length)
                        .WithContentType(contentType);

                    // Try alternative method: using direct stream
                    await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
                }

                // Return presigned URL (valid for 7 days)
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(7 * 24 * 60 * 60);

                var presignedUrl = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
                return presignedUrl;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload file to MinIO: {ex.Message}", ex);
        }
    }

    public async Task DeleteFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(objectName);

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file from MinIO: {ex.Message}", ex);
        }
    }

    public async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to ensure bucket exists in MinIO: {ex.Message}", ex);
        }
    }

    public async Task<long> GetFileSizeAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(objectName);

        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return stat.Size;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get file size from MinIO: {ex.Message}", ex);
        }
    }
}
