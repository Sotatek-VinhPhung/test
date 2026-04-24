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
            fileStream.Position = 0;

            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType),
                cancellationToken
            );

            return await _minioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(7 * 24 * 60 * 60)
            );
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

    public async Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectName,
        int expiresInSeconds = 3600,
        CancellationToken cancellationToken = default)
        {
            var args = new Minio.DataModel.Args.PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithExpiry(expiresInSeconds);

            return await _minioClient.PresignedGetObjectAsync(args);
        }

    public async Task<Stream> DownloadFileAsync(
    string bucket,
    string objectName,
    CancellationToken ct = default)
    {
        var memStream = new MemoryStream();

        var args = new Minio.DataModel.Args.GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(memStream);
            });

        await _minioClient.GetObjectAsync(args, ct);

        memStream.Position = 0;
        return memStream;
    }

    public async Task<bool> ObjectExistsAsync(
        string bucket,
        string objectName,
        CancellationToken ct = default)
    {
        try
        {
            var args = new Minio.DataModel.Args.StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(args, ct);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}
