using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class Conversation : BaseAuditableEntity
    {
        public ConversationType Type { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid? CreatedByUserId { get; set; }

        // Group settings
        public bool IsPublic { get; set; } = false;
        public string? InviteLink { get; set; }
        public int? MaxParticipants { get; set; }
        public bool OnlyAdminsCanMessage { get; set; } = false;

        // Navigation
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public Message? LastMessage { get; set; }
        public Guid? LastMessageId { get; set; }
    }

    public enum ConversationType
    {
        Private = 0,
        Group = 1,
        Channel = 2
    }
}
