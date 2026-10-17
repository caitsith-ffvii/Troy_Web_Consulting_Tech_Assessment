using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;

namespace PropertyManagement.Web.Data.Configurations;

public class ApplicationStatusHistoryConfiguration
    : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(
        EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.ChangedAtUtc)
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(h => h.NewStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(h => h.Comment)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(h => h.RentalApplicationId);

        builder.HasIndex(h => h.ChangedByUserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}