using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("Id");
        builder.Property(r => r.Code)
            .HasColumnName("Code")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasColumnName("Description")
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        // Unique constraint on Code
        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasDatabaseName("IX_Role_Code_Unique");
        
        // Relationships
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(r => r.RoleSubsystemPermissions)
            .WithOne(rsp => rsp.Role)
            .HasForeignKey(rsp => rsp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
