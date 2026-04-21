using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Persistence;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository cho entity ExportJob - quản lý các export jobs trong queue.
/// 
/// Cung cấp các operation cơ bản:
/// - GetByIdAsync: Lấy job theo ID
/// - GetPendingJobsAsync: Lấy các job chưa xử lý
/// - GetUserJobsAsync: Lấy các job của user (cho API check trạng thái)
/// - CreateAsync: Thêm job mới vào DB
/// - UpdateAsync: Cập nhật trạng thái job (khi processing/completed/failed)
/// - DeleteAsync: Soft delete job
/// 
/// All operations sử dụng async/await để không block database connection
/// </summary>
public class ExportJobRepository
{
    private readonly AppDbContext _dbContext;

    public ExportJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Lấy 1 export job theo ID.
    /// </summary>
    /// <param name="id">ID của export job</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>Export job nếu tìm thấy, null nếu không có</returns>
    public async Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _dbContext.Set<ExportJob>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Lấy tất cả export job pending (chưa xử lý).
    /// Được gọi bởi ExportWorker để lấy job từ queue.
    /// 
    /// Query logic:
    /// - Status = "Pending"
    /// - IsActive = true
    /// - Order by CreatedAt (FIFO - First In First Out)
    /// - Limit theo số worker cần xử lý
    /// </summary>
    /// <param name="limit">Số job tối đa trả về (default 1 cho 1 worker)</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>List các pending jobs sắp xếp theo thứ tự tạo</returns>
    public async Task<List<ExportJob>> GetPendingJobsAsync(
        int limit = 1,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ExportJob>()
            .Where(x => x.Status == "Pending" && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy tất cả export jobs của 1 user.
    /// Được gọi bởi API GET /api/export/my-exports để hiển thị lịch sử export.
    /// 
    /// Query logic:
    /// - UserId = specified user
    /// - IsActive = true (không hiển thị deleted jobs)
    /// - Order by CreatedAt DESC (mới nhất trước)
    /// - Optional: filter theo status
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="status">Optional: filter theo status (null = all statuses)</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>List các jobs của user, sắp xếp mới nhất trước</returns>
    public async Task<List<ExportJob>> GetUserJobsAsync(
        Guid userId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return new List<ExportJob>();

        var query = _dbContext.Set<ExportJob>()
            .Where(x => x.UserId == userId && x.IsActive);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy các export jobs đã hết hạn (ExpiresAt < now).
    /// Được gọi bởi cleanup service để xoá files cũ.
    /// </summary>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>List các jobs hết hạn</returns>
    public async Task<List<ExportJob>> GetExpiredJobsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Set<ExportJob>()
            .Where(x => x.ExpiresAt != null && x.ExpiresAt < now && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy các export jobs bị failed và chưa vượt quá MaxRetries.
    /// Được gọi bởi retry service để retry các job fail.
    /// </summary>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>List các jobs failed có thể retry</returns>
    public async Task<List<ExportJob>> GetFailedJobsForRetryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ExportJob>()
            .Where(x => x.Status == "Failed" && 
                        x.RetryCount < x.MaxRetries && 
                        x.IsActive)
            .OrderBy(x => x.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Thêm 1 export job mới vào database.
    /// Được gọi bởi ExportController khi user request export.
    /// 
    /// Khi tạo job:
    /// - Status = "Pending" (chưa xử lý)
    /// - Progress = 0 (0% tiến độ)
    /// - RetryCount = 0 (chưa thử lại)
    /// - CreatedAt = now (tự động set)
    /// </summary>
    /// <param name="job">Export job entity (chưa được track)</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>ID của job vừa tạo</returns>
    public async Task<Guid> CreateAsync(ExportJob job, CancellationToken cancellationToken = default)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        _dbContext.Set<ExportJob>().Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return job.Id;
    }

    /// <summary>
    /// Cập nhật 1 export job (gọi sau mỗi stage: pending → processing → completed/failed).
    /// 
    /// Các trường thường cập nhật:
    /// - Status (Pending → Processing → Completed)
    /// - Progress (0% → 50% → 100%)
    /// - FileUrl, Bucket, ObjectName (khi completed)
    /// - ErrorMessage (nếu failed)
    /// - CompletedAt (khi completed/failed)
    /// - StartedAt (khi đổi thành processing)
    /// </summary>
    /// <param name="job">Export job với các field đã cập nhật</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>Task hoàn tất khi cập nhật xong</returns>
    public async Task UpdateAsync(ExportJob job, CancellationToken cancellationToken = default)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        // Attach entity và mark as modified
        _dbContext.Set<ExportJob>().Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Soft delete export job (set IsActive = false).
    /// Không xoá vật lý khỏi database, chỉ đánh dấu là xoá.
    /// Query mặc định sẽ filter IsActive = true, nên không hiển thị.
    /// </summary>
    /// <param name="id">ID của export job cần xoá</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>true nếu xoá thành công, false nếu job không tìm thấy</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        var job = await _dbContext.Set<ExportJob>()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (job == null)
            return false;

        job.IsActive = false;
        _dbContext.Set<ExportJob>().Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Lấy thống kê export jobs (dùng cho admin dashboard, monitoring).
    /// </summary>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>Object chứa số lượng jobs theo status</returns>
    public async Task<ExportJobStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _dbContext.Set<ExportJob>()
            .Where(x => x.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new ExportJobStatistics
        {
            PendingCount = jobs.Count(x => x.Status == "Pending"),
            ProcessingCount = jobs.Count(x => x.Status == "Processing"),
            CompletedCount = jobs.Count(x => x.Status == "Completed"),
            FailedCount = jobs.Count(x => x.Status == "Failed"),
            TotalCount = jobs.Count,
            AverageProcessingTimeSeconds = jobs
                .Where(x => x.CompletedAt.HasValue && x.StartedAt.HasValue)
                .Select(x => (x.CompletedAt!.Value - x.StartedAt!.Value).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average()
        };
    }

    /// <summary>
    /// Check xem job có tồn tại và thuộc về user không (authorization check).
    /// Dùng trước khi trả file download cho user.
    /// </summary>
    /// <param name="jobId">ID của export job</param>
    /// <param name="userId">ID của user requesting</param>
    /// <param name="cancellationToken">Token hủy bỏ</param>
    /// <returns>true nếu job tồn tại và thuộc về user</returns>
    public async Task<bool> UserOwnsJobAsync(
        Guid jobId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty || userId == Guid.Empty)
            return false;

        return await _dbContext.Set<ExportJob>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == jobId && x.UserId == userId && x.IsActive, cancellationToken);
    }
}

/// <summary>
/// DTO cho thống kê export jobs (dùng cho admin monitoring)
/// </summary>
public class ExportJobStatistics
{
    /// <summary>Số job pending (chưa xử lý)</summary>
    public int PendingCount { get; set; }

    /// <summary>Số job đang processing</summary>
    public int ProcessingCount { get; set; }

    /// <summary>Số job hoàn tất</summary>
    public int CompletedCount { get; set; }

    /// <summary>Số job lỗi</summary>
    public int FailedCount { get; set; }

    /// <summary>Tổng số job</summary>
    public int TotalCount { get; set; }

    /// <summary>Thời gian xử lý trung bình (seconds)</summary>
    public double AverageProcessingTimeSeconds { get; set; }
}
