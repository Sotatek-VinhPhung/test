using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration cho entity ExportJob.
/// 
/// ExportJob là entity quản lý trạng thái của export jobs trong queue.
/// Mỗi job có vòng đời: Pending → Processing → Completed (hoặc Failed)
/// 
/// Database table: export_jobs (snake_case convention)
/// </summary>
public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        // ============ TABLE & PRIMARY KEY ============
        // Đặt tên bảng theo snake_case convention (như các bảng khác)
        builder.ToTable("export_jobs");

        // Primary key
        builder.HasKey(x => x.Id);

        // ============ PROPERTIES ============

        // FileName - Tên file export (yêu cầu)
        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("file_name");

        // Format - Định dạng file (Excel/Word/PDF)
        // Lưu dạng string ("Excel", "Word", "PDF")
        builder.Property(x => x.Format)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("format");

        // Status - Trạng thái job (Pending/Processing/Completed/Failed)
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("status")
            .HasDefaultValue("Pending");

        // Progress - Tiến độ xử lý (0-100%)
        builder.Property(x => x.Progress)
            .IsRequired()
            .HasColumnName("progress")
            .HasDefaultValue(0);

        // FileUrl - URL download file từ MinIO (nếu thành công)
        builder.Property(x => x.FileUrl)
            .HasMaxLength(500)
            .HasColumnName("file_url");

        // Bucket - Tên bucket MinIO
        builder.Property(x => x.Bucket)
            .HasMaxLength(255)
            .HasColumnName("bucket");

        // ObjectName - Tên object trong MinIO
        builder.Property(x => x.ObjectName)
            .HasMaxLength(500)
            .HasColumnName("object_name");

        // FileSize - Kích thước file (bytes)
        builder.Property(x => x.FileSize)
            .IsRequired()
            .HasColumnName("file_size")
            .HasDefaultValue(0L);

        // ErrorMessage - Thông báo lỗi (nếu job failed)
        builder.Property(x => x.ErrorMessage)
            .HasColumnType("text")
            .HasColumnName("error_message");

        // Payload - Dữ liệu export (JSON string chứa danh sách records)
        // Lưu dạng JSON để có thể query/filter trong PostgreSQL
        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .HasColumnName("payload");

        // Note - Ghi chú thêm từ user
        builder.Property(x => x.Note)
            .HasMaxLength(500)
            .HasColumnName("note");

        // StartedAt - Thời điểm worker bắt đầu xử lý
        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at");

        // CompletedAt - Thời điểm xử lý xong
        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        // ExpiresAt - Thời điểm file hết hạn (tự động xoá)
        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        // RetryCount - Số lần thử lại hiện tại
        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        // MaxRetries - Số lần thử lại tối đa (mặc định 3)
        builder.Property(x => x.MaxRetries)
            .IsRequired()
            .HasColumnName("max_retries")
            .HasDefaultValue(3);

        // UserId - Foreign key tới Users table
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        // ============ INHERITED BASE ENTITY PROPERTIES ============
        // Các property từ BaseEntity cũng cần map

        // Id - UUID, auto generated
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // CreatedAt - Tự động set bởi SaveChangesAsync
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt - Tự động set bởi SaveChangesAsync
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // IsActive - Soft delete flag (mặc định true)
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // ============ RELATIONSHIPS ============

        // Foreign Key: UserId → User.Id
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // ============ INDEXES ============
        // Các index để tối ưu query performance

        // Index 1: UserId - Để query các job của user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_export_jobs_user_id");

        // Index 2: Status - Để query các job pending/processing
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("idx_export_jobs_status");

        // Index 3: CreatedAt - Để query theo ngày tạo
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_export_jobs_created_at");

        // Index 4: ExpiresAt - Để cleanup hết hạn
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_export_jobs_expires_at");

        // Index 5: IsActive - Để exclude soft-deleted
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("idx_export_jobs_is_active");

        // Index 6: Composite index (UserId + Status) - Query pending jobs của user
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("idx_export_jobs_user_status");

        // Index 7: Composite index (Status + CreatedAt) - Query recent jobs by status
        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("idx_export_jobs_status_created");

        // ============ QUERY FILTERS ============
        // Tự động filter IsActive = true trong queries (soft delete)
        builder.HasQueryFilter(x => x.IsActive);
    }
}
