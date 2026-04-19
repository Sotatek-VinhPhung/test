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

        builder.ToTable("ExportedFiles");

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_ExportedFiles_UserId");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ExportedFiles_CreatedAt");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_ExportedFiles_UserId_CreatedAt");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_ExportedFiles_ExpiresAt");
    }
}
