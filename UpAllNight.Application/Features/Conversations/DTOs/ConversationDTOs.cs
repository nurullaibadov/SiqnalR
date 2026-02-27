using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Messages.DTOs;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Features.Conversations.DTOs
{
    public class ConversationDto
    {
        public Guid Id { get; set; }
        public ConversationType Type { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public bool IsMuted { get; set; }
        public bool IsArchived { get; set; }
        public bool IsPinned { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class ParticipantDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public ParticipantRole Role { get; set; }
        public string? Nickname { get; set; }
    }

    public record CreatePrivateConversationDto([Required] Guid TargetUserId);

    public record CreateGroupConversationDto(
        [Required][StringLength(100, MinimumLength = 1)] string Name,
        string? Description,
        [Required][MinLength(1)] List<Guid> ParticipantIds,
        bool IsPublic = false,
        int? MaxParticipants = null
    );

    public record UpdateGroupDto(
        string? Name,
        string? Description,
        bool? OnlyAdminsCanMessage,
        int? MaxParticipants
    );

    public record AddParticipantDto([Required] Guid UserId);
    public record RemoveParticipantDto([Required] Guid UserId);
    public record UpdateParticipantRoleDto([Required] Guid UserId, [Required] ParticipantRole Role);
}
