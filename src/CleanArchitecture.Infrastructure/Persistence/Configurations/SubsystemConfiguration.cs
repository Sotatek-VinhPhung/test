using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class SubsystemConfiguration : IEntityTypeConfiguration<Subsystem>
{
    public void Configure(EntityTypeBuilder<Subsystem> builder)
    {
        builder.ToTable("subsystems");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("Id");
        builder.Property(s => s.Code)
            .HasColumnName("Code")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasColumnName("Description")
            .HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("CreatedAt")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Unique constraint on Code
        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("IX_Subsystem_Code_Unique");
        
        // Relationships
        builder.HasMany(s => s.RoleSubsystemPermissions)
            .WithOne(rsp => rsp.Subsystem)
            .HasForeignKey(rsp => rsp.SubsystemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
