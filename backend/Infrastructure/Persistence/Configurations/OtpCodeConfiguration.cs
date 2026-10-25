using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatApp.Domain.Entities;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Code).HasMaxLength(10).IsRequired();
        builder.Property(o => o.Purpose).HasConversion<string>().IsRequired();
        builder.Property(o => o.ExpiresAt).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.VerifiedAt);
        builder.Property(o => o.AttemptCount).IsRequired();
        builder.Property(o => o.MaxAttempts).IsRequired();

        builder.HasIndex(o => new { o.PhoneNumber, o.Purpose });
        builder.HasIndex(o => o.ExpiresAt);
    }
}