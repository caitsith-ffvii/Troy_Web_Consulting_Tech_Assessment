
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Data.Configurations;

public class ApplicantConfiguration : IEntityTypeConfiguration<Applicant>
{
    public void Configure(EntityTypeBuilder<Applicant> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(a => a.CurrentAddress)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(a => a.UserId)
            .IsUnique();

        builder.HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<Applicant>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
