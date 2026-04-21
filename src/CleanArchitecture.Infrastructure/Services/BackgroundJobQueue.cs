using System.Threading.Channels;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Services;

/// <summary>
/// Implement hệ thống queue background jobs sử dụng System.Threading.Channels.
/// 
/// Channel<T> là một bounded queue, hỗ trợ:
/// - Async producer-consumer pattern
/// - Backpressure handling (nếu queue full, EnqueueAsync sẽ wait)
/// - Graceful shutdown (CompleteWriter + DequeueAsync sẽ return null)
/// - Thread-safe operations
/// 
/// Capacity = 100 (tối đa 100 jobs trong queue cùng lúc)
/// Nếu vượt quá, caller phải chờ cho đến khi worker xử lý xong
/// </summary>
public class BackgroundJobQueue : IBackgroundJobQueue, IAsyncDisposable
{
    /// <summary>
    /// Channel chứa các jobs (delegates) cần xử lý
    /// </summary>
    private readonly Channel<Func<CancellationToken, Task>> _channel;

    /// <summary>
    /// Để đọc từ channel
    /// </summary>
    private readonly ChannelReader<Func<CancellationToken, Task>> _reader;

    /// <summary>
    /// Để ghi vào channel
    /// </summary>
    private readonly ChannelWriter<Func<CancellationToken, Task>> _writer;

    /// <summary>
    /// Khởi tạo queue với capacity mặc định = 100 jobs
    /// </summary>
    /// <param name="capacity">Số lượng job tối đa có thể chứa trong queue</param>
    public BackgroundJobQueue(int capacity = 100)
    {
        // Tạo channel với bounded capacity
        var options = new BoundedChannelOptions(capacity)
        {
            // Nếu queue full và tiếp tục EnqueueAsync, sẽ throw OperationCanceledException
            FullMode = BoundedChannelFullMode.Wait
        };

        _channel = Channel.CreateBounded<Func<CancellationToken, Task>>(options);
        _reader = _channel.Reader;
        _writer = _channel.Writer;
    }

    /// <summary>
    /// Thêm 1 job vào queue để background worker xử lý.
    /// 
    /// Phương thức này:
    /// 1. Không block thread (async)
    /// 2. Nếu queue chưa full → job được thêm vào ngay
    /// 3. Nếu queue full → wait cho đến khi worker xử lý xong job (backpressure)
    /// 4. Nếu cancellationToken được cancel → throw OperationCanceledException
    /// 5. Nếu channel đã closed (shutting down) → throw ChannelClosedException
    /// 
    /// Được gọi từ ExportController khi user request export
    /// </summary>
    /// <param name="workItem">Async delegate chứa logic export (Func<CancellationToken, Task>)</param>
    /// <param name="cancellationToken">Token để hủy bỏ nếu cần</param>
    /// <returns>Task hoàn tất khi job được ghi vào channel</returns>
    /// <exception cref="OperationCanceledException">Nếu cancellationToken bị cancel</exception>
    /// <exception cref="ChannelClosedException">Nếu application đang shutdown</exception>
    public async Task EnqueueAsync(
        Func<CancellationToken, Task> workItem,
        CancellationToken cancellationToken = default)
    {
        if (workItem is null)
            throw new ArgumentNullException(nameof(workItem));

        try
        {
            // Ghi job vào channel (async, có thể wait nếu full)
            await _writer.WriteAsync(workItem, cancellationToken);
        }
        catch (ChannelClosedException ex)
        {
            // Application đang shutdown
            throw new InvalidOperationException("Background job queue is closed (application shutting down)", ex);
        }
    }

    /// <summary>
    /// Lấy 1 job từ queue để xử lý (được gọi bởi ExportWorker).
    /// 
    /// Phương thức này:
    /// 1. Block async cho đến khi có job từ queue
    /// 2. Nếu queue empty nhưng channel chưa close → wait
    /// 3. Nếu channel đã close → return null
    /// 4. Nếu cancellationToken cancel → throw OperationCanceledException
    /// 
    /// ExportWorker sẽ loop gọi method này để liên tục lấy jobs
    /// </summary>
    /// <param name="cancellationToken">Token để shutdown gracefully</param>
    /// <returns>
    /// Async delegate (job) nếu có job trong queue
    /// Null nếu channel đã đóng (application shutting down)
    /// </returns>
    /// <exception cref="OperationCanceledException">Nếu cancellationToken bị cancel</exception>
    public async Task<Func<CancellationToken, Task>?> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Đợi job từ channel
            if (await _reader.WaitToReadAsync(cancellationToken))
            {
                // Có job để đọc
                if (_reader.TryRead(out var workItem))
                {
                    return workItem;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // cancellationToken bị cancel (application shutting down)
            throw;
        }

        // Channel đã close, không còn job nào
        return null;
    }

    /// <summary>
    /// Đóng channel khi application shutdown.
    /// Không nhận job mới nữa, worker sẽ dùng drain hết các job còn lại
    /// </summary>
    public void CompleteWriter() => _writer.TryComplete();

    /// <summary>
    /// Kiểm tra xem channel đã complete chưa
    /// (được dùng cho monitoring, health check)
    /// </summary>
    public bool IsCompleted => _reader.Completion.IsCompleted;

    /// <summary>
    /// Lấy số lượng job hiện tại trong queue
    /// (Channel<T> không expose Count, nhưng có thể monitoring bằng khác)
    /// </summary>
    public int Count => _channel.Reader.Count;

    /// <summary>
    /// Graceful shutdown: đóng channel và chờ worker drain hết jobs
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _writer.TryComplete();

        try
        {
            // Chờ worker xử lý xong hết các jobs trong queue
            await _reader.Completion;
        }
        catch
        {
            // Nếu worker throw exception, vẫn cần cleanup
        }
    }
}

/// <summary>
/// Extension để registration vào DI container
/// </summary>
public static class BackgroundJobQueueExtensions
{
    /// <summary>
    /// Thêm background job queue vào DI container
    /// Được gọi trong Program.cs
    /// </summary>
    public static IServiceCollection AddBackgroundJobQueue(
        this IServiceCollection services,
        int capacity = 100)
    {
        services.AddSingleton<IBackgroundJobQueue>(_ => new BackgroundJobQueue(capacity));
        return services;
    }
}
