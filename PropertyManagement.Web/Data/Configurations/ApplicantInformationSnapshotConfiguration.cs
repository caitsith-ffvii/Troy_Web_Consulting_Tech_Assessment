
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class ApplicantInformationSnapshotConfiguration
    : IEntityTypeConfiguration<ApplicantInformationSnapshot>
{
    public void Configure(
        EntityTypeBuilder<ApplicantInformationSnapshot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.CurrentAddress)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(s => s.RentalApplicationId)
            .IsUnique();
    }
}
