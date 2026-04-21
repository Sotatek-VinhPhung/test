using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class RoleSubsystemPermissionConfiguration : IEntityTypeConfiguration<RoleSubsystemPermission>
{
    public void Configure(EntityTypeBuilder<RoleSubsystemPermission> builder)
    {
        builder.ToTable("role_subsystem_permissions");
        // Composite primary key
        builder.HasKey(rsp => new { rsp.RoleId, rsp.SubsystemId });

        builder.Property(rsp => rsp.RoleId).HasColumnName("RoleId");
        builder.Property(rsp => rsp.SubsystemId).HasColumnName("SubsystemId");
        builder.Property(rsp => rsp.Flags)
            .HasColumnName("Flags")
            .HasDefaultValue(0L);

        builder.Property(rsp => rsp.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(rsp => rsp.Role)
            .WithMany(r => r.RoleSubsystemPermissions)
            .HasForeignKey(rsp => rsp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(rsp => rsp.Subsystem)
            .WithMany(s => s.RoleSubsystemPermissions)
            .HasForeignKey(rsp => rsp.SubsystemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes for common queries
        builder.HasIndex(rsp => rsp.SubsystemId)
            .HasDatabaseName("IX_RoleSubsystemPermission_SubsystemId");
        
        builder.HasIndex(rsp => rsp.RoleId)
            .HasDatabaseName("IX_RoleSubsystemPermission_RoleId");
    }
}
