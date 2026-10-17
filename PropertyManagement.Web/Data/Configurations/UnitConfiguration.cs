
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.MonthlyRent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(u => u.IsArchived)
            .IsRequired();

        // Unit numbers must be unique within each property.
        builder.HasIndex(u => new { u.PropertyId, u.UnitNumber })
            .IsUnique();

        builder.HasIndex(u => u.UnitTypeId);

        // Preserve units and their relationships when a property is archived.
        builder.HasOne(u => u.Property)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unit types are deactivated rather than deleted.
        builder.HasOne(u => u.UnitType)
            .WithMany(t => t.Units)
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
