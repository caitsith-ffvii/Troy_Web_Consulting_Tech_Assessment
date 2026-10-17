using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class PropertyManagerConfiguration
    : IEntityTypeConfiguration<PropertyManager>
{
    public void Configure(EntityTypeBuilder<PropertyManager> builder)
    {
        builder.HasKey(pm => pm.Id);

        builder.HasIndex(pm => pm.UserId)
            .IsUnique();

        builder.HasOne(pm => pm.User)
            .WithOne()
            .HasForeignKey<PropertyManager>(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
