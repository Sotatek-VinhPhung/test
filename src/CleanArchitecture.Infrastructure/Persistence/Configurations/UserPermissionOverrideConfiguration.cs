using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("user_permission_overrides");

        builder.HasKey(up => new { up.UserId, up.Module });

        builder.Property(up => up.UserId).HasColumnName("UserId");
        builder.Property(up => up.Module)
            .HasColumnName("Module")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(up => up.Flags)
            .HasColumnName("Flags")
            .IsRequired();

        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
