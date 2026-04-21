using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho Region entity
/// </summary>
public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("regions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("Id");
        builder.Property(r => r.Code)
            .HasColumnName("Code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Country)
            .HasColumnName("Country")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Unique constraint on Code
        builder.HasIndex(r => r.Code)
            .IsUnique();

        // Foreign keys
        builder.HasMany(r => r.Companies)
            .WithOne(c => c.Region)
            .HasForeignKey(c => c.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Users)
            .WithOne(u => u.Region)
            .HasForeignKey(u => u.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.RoleScopes)
            .WithOne(s => s.Region)
            .HasForeignKey(s => s.RegionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration cho Company entity
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("Id");
        builder.Property(c => c.RegionId).HasColumnName("RegionId");
        builder.Property(c => c.Code)
            .HasColumnName("Code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TaxId)
            .HasColumnName("TaxId")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Unique constraint on Code and TaxId
        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.HasIndex(c => c.TaxId)
            .IsUnique();

        // Foreign keys
        builder.HasOne(c => c.Region)
            .WithMany(r => r.Companies)
            .HasForeignKey(c => c.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Departments)
            .WithOne(d => d.Company)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Users)
            .WithOne(u => u.Company)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.RoleScopes)
            .WithOne(s => s.Company)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration cho Department entity
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("Id");
        builder.Property(d => d.CompanyId).HasColumnName("CompanyId");
        builder.Property(d => d.Code)
            .HasColumnName("Code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Unique constraint on Code (per Company)
        builder.HasIndex(d => new { d.CompanyId, d.Code })
            .IsUnique();

        // Foreign keys
        builder.HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Users)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.RoleScopes)
            .WithOne(s => s.Department)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration cho RoleOrganizationScope entity
/// Giới hạn phạm vi của một role (ABAC - Attribute-Based Access Control)
/// </summary>
public class RoleOrganizationScopeConfiguration : IEntityTypeConfiguration<RoleOrganizationScope>
{
    public void Configure(EntityTypeBuilder<RoleOrganizationScope> builder)
    {
        builder.ToTable("role_organization_scopes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("Id");
        builder.Property(s => s.RoleId).HasColumnName("RoleId");
        builder.Property(s => s.RegionId).HasColumnName("RegionId");
        builder.Property(s => s.CompanyId).HasColumnName("CompanyId");
        builder.Property(s => s.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(s => s.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign keys
        builder.HasOne(s => s.Role)
            .WithMany(r => r.OrganizationScopes)
            .HasForeignKey(s => s.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Region)
            .WithMany(r => r.RoleScopes)
            .HasForeignKey(s => s.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Company)
            .WithMany(c => c.RoleScopes)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Department)
            .WithMany(d => d.RoleScopes)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraint: một role chỉ có một scope cho mỗi combination
        builder.HasIndex(s => new { s.RoleId, s.RegionId, s.CompanyId, s.DepartmentId })
            .IsUnique();
    }
}

/// <summary>
/// Updated User configuration để support organizational hierarchy
/// </summary>
public class UserConfigurationUpdated : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Organizational hierarchy
        builder.HasOne(u => u.Region)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Roles relationship
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
