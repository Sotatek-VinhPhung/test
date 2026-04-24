using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasColumnName("Type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasColumnName("Description").HasColumnType("text");
        builder.Property(x => x.Bucket).HasColumnName("Bucket").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ObjectName).HasColumnName("ObjectName").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("FileName").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Size).HasColumnName("Size").IsRequired();
        builder.Property(x => x.Version).HasColumnName("Version").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Category).HasColumnName("Category").HasMaxLength(100);
        builder.Property(x => x.UploadedBy).HasColumnName("UploadedBy").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");

        builder.HasIndex(x => new { x.Type, x.Name }).HasDatabaseName("idx_templates_type_name");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("idx_templates_is_active");
        builder.HasIndex(x => x.Category).HasDatabaseName("idx_templates_category");
    }
}