using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("Id");
        builder.Property(u => u.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasColumnName("Email").HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("PasswordHash").IsRequired();
        builder.Property(u => u.Role).HasColumnName("Role").HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.RefreshToken).HasColumnName("RefreshToken").HasMaxLength(256);
        builder.Property(u => u.RegionId).HasColumnName("RegionId");
        builder.Property(u => u.CompanyId).HasColumnName("CompanyId");
        builder.Property(u => u.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(u => u.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(u => u.UpdatedAt).HasColumnName("UpdatedAt");
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
