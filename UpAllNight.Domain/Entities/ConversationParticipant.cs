using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class ConversationParticipant : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public ParticipantRole Role { get; set; } = ParticipantRole.Member;
        public bool IsMuted { get; set; } = false;
        public DateTime? MutedUntil { get; set; }
        public bool IsArchived { get; set; } = false;
        public bool IsPinned { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
        public DateTime? LastReadAt { get; set; }
        public Guid? LastReadMessageId { get; set; }
        public bool HasLeft { get; set; } = false;
        public int UnreadCount { get; set; } = 0;
        public string? Nickname { get; set; }
    }

    public enum ParticipantRole
    {
        Member = 0,
        Admin = 1,
        Owner = 2
    }
}
