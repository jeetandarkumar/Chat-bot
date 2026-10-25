using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatApp.Domain.Entities;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");
        builder.HasKey(cp => new { cp.ConversationId, cp.UserId });

        builder.Property(cp => cp.JoinedAt).IsRequired();
        builder.Property(cp => cp.LeftAt);
        builder.Property(cp => cp.IsAdmin).IsRequired();
        builder.Property(cp => cp.IsMuted).IsRequired();
        builder.Property(cp => cp.MutedUntil);
        builder.Property(cp => cp.UnreadCount).IsRequired();
        builder.Property(cp => cp.LastReadAt);

        builder.HasOne(cp => cp.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cp => cp.UserId);
    }
}