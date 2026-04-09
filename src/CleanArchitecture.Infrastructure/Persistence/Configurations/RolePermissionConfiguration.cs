using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => new { rp.Role, rp.Module });

        builder.Property(rp => rp.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(rp => rp.Module)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(rp => rp.Flags)
            .IsRequired();
    }
}
