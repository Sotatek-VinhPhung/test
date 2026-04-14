using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class SubsystemConfiguration : IEntityTypeConfiguration<Subsystem>
{
    public void Configure(EntityTypeBuilder<Subsystem> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.Description)
            .HasMaxLength(500);
        
        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);
        
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
