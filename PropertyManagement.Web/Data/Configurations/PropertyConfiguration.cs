
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.IsArchived)
            .IsRequired();

        builder.HasOne(p => p.PropertyManager)
            .WithMany(pm => pm.Properties)
            .HasForeignKey(p => p.PropertyManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
