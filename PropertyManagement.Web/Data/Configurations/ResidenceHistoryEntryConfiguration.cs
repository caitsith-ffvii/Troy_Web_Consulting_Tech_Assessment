
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class ResidenceHistoryEntryConfiguration
    : IEntityTypeConfiguration<ResidenceHistoryEntry>
{
    public void Configure(
        EntityTypeBuilder<ResidenceHistoryEntry> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.LandlordName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.LandlordPhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.MoveInDate)
            .IsRequired();

        builder.Property(r => r.MoveOutDate)
            .IsRequired(false);

        builder.HasIndex(r => r.RentalApplicationId);
    }
}
