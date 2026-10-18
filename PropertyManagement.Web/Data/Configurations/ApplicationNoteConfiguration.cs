using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;

namespace PropertyManagement.Web.Data.Configurations;

public class ApplicationNoteConfiguration
    : IEntityTypeConfiguration<ApplicationNote>
{
    public const int BodyMaxLength = 2000;

    public void Configure(EntityTypeBuilder<ApplicationNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(BodyMaxLength);

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired();

        builder.Property(n => n.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(n => n.RentalApplicationId);

        // One-way: no Notes collection on RentalApplication.
        builder.HasOne(n => n.RentalApplication)
            .WithMany()
            .HasForeignKey(n => n.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
