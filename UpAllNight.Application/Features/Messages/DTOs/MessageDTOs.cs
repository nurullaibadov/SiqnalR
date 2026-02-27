using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Features.Messages.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderUserName { get; set; } = null!;
        public string? SenderProfilePicture { get; set; }
        public string? Content { get; set; }
        public MessageType Type { get; set; }
        public MessageStatus Status { get; set; }
        public bool IsEdited { get; set; }
        public bool IsForwarded { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public MessageDto? ReplyToMessage { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
        public List<ReactionDto> Reactions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public AttachmentType Type { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }
    }

    public class ReactionDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Emoji { get; set; } = null!;
    }

    public record SendMessageRequestDto(
        [Required] Guid ConversationId,
        string? Content,
        MessageType Type = MessageType.Text,
        Guid? ReplyToMessageId = null
    );

    public record EditMessageRequestDto([Required] string Content);

    public record ForwardMessageRequestDto(
        [Required] Guid MessageId,
        [Required] Guid TargetConversationId
    );
}
