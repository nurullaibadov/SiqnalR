using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class Message : BaseAuditableEntity
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public Guid SenderId { get; set; }
        public User Sender { get; set; } = null!;
        public string? Content { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public MessageStatus Status { get; set; } = MessageStatus.Sent;

        // Reply
        public Guid? ReplyToMessageId { get; set; }
        public Message? ReplyToMessage { get; set; }

        // Forward
        public bool IsForwarded { get; set; } = false;
        public Guid? OriginalMessageId { get; set; }

        // Edit
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public string? OriginalContent { get; set; }

        // Delivery
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation
        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageStatus_Tracker> StatusTrackers { get; set; } = new List<MessageStatus_Tracker>();
        public ICollection<Message> Replies { get; set; } = new List<Message>();
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Document = 4,
        Location = 5,
        Contact = 6,
        Sticker = 7,
        Gif = 8,
        System = 9
    }

    public enum MessageStatus
    {
        Sending = 0,
        Sent = 1,
        Delivered = 2,
        Read = 3,
        Failed = 4
    }
}
