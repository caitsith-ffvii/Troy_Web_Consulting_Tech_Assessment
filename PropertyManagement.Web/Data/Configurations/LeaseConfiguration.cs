using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartDate)
            .IsRequired();

        builder.Property(l => l.EndDate)
            .IsRequired();

        builder.Property(l => l.AgreedRent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(l => l.UnitId);

        builder.HasIndex(l => l.ApplicantId);

        builder.HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Applicant)
            .WithMany()
            .HasForeignKey(l => l.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}