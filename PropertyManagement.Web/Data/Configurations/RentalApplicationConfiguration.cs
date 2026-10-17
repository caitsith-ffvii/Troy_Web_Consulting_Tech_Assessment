
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class RentalApplicationConfiguration
    : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(a => new { a.ApplicantId, a.Status });

        builder.HasIndex(a => new { a.UnitId, a.Status });

        builder.HasOne(a => a.Applicant)
            .WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Unit)
            .WithMany()
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApplicantInformation)
            .WithOne(s => s.RentalApplication)
            .HasForeignKey<ApplicantInformationSnapshot>(
                s => s.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ResidenceHistory)
            .WithOne(r => r.RentalApplication)
            .HasForeignKey(r => r.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.StatusHistory)
            .WithOne(h => h.RentalApplication)
            .HasForeignKey(h => h.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
