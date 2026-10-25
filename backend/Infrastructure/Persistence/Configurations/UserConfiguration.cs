using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatApp.Domain.Entities;
using ChatApp.Domain.ValueObjects;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.PhoneNumber)
            .HasConversion(
                v => v.Value,
                v => PhoneNumber.Parse(v))
            .HasMaxLength(20)
            .IsRequired();
            
        builder.HasIndex(u => u.PhoneNumber).IsUnique();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(100);

        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.LastSeenAt);

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.IsDeleted).IsRequired();
        builder.Property(u => u.IsPhoneNumberVerified).IsRequired();

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}