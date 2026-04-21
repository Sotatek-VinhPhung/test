namespace CleanArchitecture.Application.Interfaces;

/// <summary>
/// Giao diện abstraction cho hệ thống queue background jobs.
/// Sử dụng Channel<T> từ System.Threading.Channels để xử lý async producer-consumer pattern.
/// 
/// Queue này là in-memory, không phục vụ cho distributed systems.
/// Nếu ứng dụng bị restart, các job chưa xong sẽ mất.
/// Để production-grade distributed queue, sử dụng MassTransit, NServiceBus, hoặc Kafka.
/// </summary>
public interface IBackgroundJobQueue
{
    /// <summary>
    /// Thêm 1 job vào queue để xử lý bất đồng bộ.
    /// Phương thức này không block, job sẽ được xử lý bởi background worker.
    /// </summary>
    /// <param name="workItem">Delegate chứa logic job cần thực hiện</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>Task hoàn tất khi job được thêm vào queue</returns>
    /// <example>
    /// await _queue.EnqueueAsync(async ct => {
    ///     // Xử lý export file
    ///     await _exportService.ProcessExportAsync(jobId, ct);
    /// }, cancellationToken);
    /// </example>
    Task EnqueueAsync(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy 1 job từ queue để xử lý.
    /// Phương thức này sẽ block cho đến khi có job hoặc cancellationToken bị huỷ.
    /// 
    /// Được gọi bởi background worker service trong loop.
    /// </summary>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>
    /// Delegate chứa logic job nếu có job trong queue.
    /// Null nếu channel đã bị đóng và không còn job.
    /// </returns>
    /// <example>
    /// while (!cancellationToken.IsCancellationRequested)
    /// {
    ///     var workItem = await _queue.DequeueAsync(cancellationToken);
    ///     if (workItem is null) break; // Channel đã đóng
    ///     await workItem(cancellationToken);
    /// }
    /// </example>
    Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken = default);
}
