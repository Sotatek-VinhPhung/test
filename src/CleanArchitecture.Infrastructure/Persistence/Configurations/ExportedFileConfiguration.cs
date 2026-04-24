using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ExportedFile entity.
/// </summary>
public class ExportedFileConfiguration : IEntityTypeConfiguration<ExportedFile>
{
    public void Configure(EntityTypeBuilder<ExportedFile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.Bucket)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Size)
            .IsRequired();

        builder.Property(x => x.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ObjectName)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.ToTable("exported_files");

        // Foreign key
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_exported_files_user_id");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_exported_files_created_at");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_exported_files_expires_at");

        builder.HasIndex(x => x.Bucket)
            .HasDatabaseName("idx_exported_files_bucket");

        builder.Property(x => x.CacheKey)
        .HasMaxLength(100)
        .HasColumnName("CacheKey");

            builder.Property(x => x.TemplateVersion)
                .HasColumnName("TemplateVersion");

            builder.HasIndex(x => x.CacheKey)
                .HasDatabaseName("idx_exported_files_cache_key");
    }
}
