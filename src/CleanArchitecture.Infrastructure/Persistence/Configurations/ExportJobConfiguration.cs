using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration cho entity ExportJob.
/// 
/// ExportJob quản lý trạng thái của export jobs trong Kafka queue.
/// Vòng đời: Pending → Processing → Completed (hoặc Failed)
/// 
/// Naming: table snake_case, column PascalCase (khớp với các bảng khác).
/// </summary>
public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        // ============ TABLE & PRIMARY KEY ============
        builder.ToTable("export_jobs");
        builder.HasKey(x => x.Id);

        // ============ BASE ENTITY PROPERTIES ============
        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // ============ DOMAIN PROPERTIES ============

        // UserId - Foreign key tới users.Id
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnName("UserId");

        // JobType - "Export" | "Preview"
        builder.Property(x => x.JobType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("JobType");

        // Status - Pending | Processing | Completed | Failed
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("Status")
            .HasDefaultValue("Pending");

        // RequestJson - JSON payload (ExportDataRequest / PreviewPdfRequest)
        builder.Property(x => x.RequestJson)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("RequestJson");

        // ResultFileId - Link tới exported_files.Id (nullable)
        builder.Property(x => x.ResultFileId)
            .HasColumnName("ResultFileId");

        // ResultUrl - URL download file từ MinIO
        builder.Property(x => x.ResultUrl)
            .HasMaxLength(2048)
            .HasColumnName("ResultUrl");

        // ResultFileName - Tên file output
        builder.Property(x => x.ResultFileName)
            .HasMaxLength(255)
            .HasColumnName("ResultFileName");

        // ErrorMessage - Thông báo lỗi nếu failed
        builder.Property(x => x.ErrorMessage)
            .HasColumnType("text")
            .HasColumnName("ErrorMessage");

        // StartedAt - Thời điểm worker bắt đầu xử lý
        builder.Property(x => x.StartedAt)
            .HasColumnName("StartedAt");

        // CompletedAt - Thời điểm hoàn thành
        builder.Property(x => x.CompletedAt)
            .HasColumnName("CompletedAt");

        // ============ RELATIONSHIPS ============
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // ============ INDEXES ============
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_export_jobs_user_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("idx_export_jobs_status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_export_jobs_created_at");
    }
}