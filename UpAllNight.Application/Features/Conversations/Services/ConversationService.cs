using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Conversations.DTOs;
using UpAllNight.Application.Features.Messages.DTOs;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Conversations.Services
{
    public interface IConversationService
    {
        Task<Result<ConversationDto>> CreatePrivateConversationAsync(Guid userId, Guid targetUserId, CancellationToken ct = default);
        Task<Result<ConversationDto>> CreateGroupConversationAsync(Guid creatorId, CreateGroupConversationDto request, CancellationToken ct = default);
        Task<Result<IEnumerable<ConversationDto>>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
        Task<Result<ConversationDto>> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result<PagedResult<MessageDto>>> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken ct = default);
        Task<Result> UpdateGroupAsync(Guid conversationId, Guid userId, UpdateGroupDto request, CancellationToken ct = default);
        Task<Result<string>> UploadGroupAvatarAsync(Guid conversationId, Guid userId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default);
        Task<Result> AddParticipantAsync(Guid conversationId, Guid userId, Guid targetUserId, CancellationToken ct = default);
        Task<Result> RemoveParticipantAsync(Guid conversationId, Guid userId, Guid targetUserId, CancellationToken ct = default);
        Task<Result> LeaveGroupAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result> UpdateParticipantRoleAsync(Guid conversationId, Guid userId, Guid targetUserId, ParticipantRole role, CancellationToken ct = default);
        Task<Result> MuteConversationAsync(Guid conversationId, Guid userId, DateTime? until, CancellationToken ct = default);
        Task<Result> UnmuteConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result> ArchiveConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result> PinConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result> DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result<string>> GenerateInviteLinkAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result<ConversationDto>> JoinByInviteLinkAsync(string inviteLink, Guid userId, CancellationToken ct = default);
    }

    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly Application.Interfaces.Services.IFileService _fileService;

        public ConversationService(IUnitOfWork uow, IMapper mapper, Application.Interfaces.Services.IFileService fileService)
        {
            _uow = uow;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<Result<ConversationDto>> CreatePrivateConversationAsync(Guid userId, Guid targetUserId, CancellationToken ct = default)
        {
            if (userId == targetUserId)
                return Result<ConversationDto>.Failure("Kendinizle konuşma başlatamazsınız.");

            // Blok kontrolü
            var isBlocked = await _uow.Repository<UserBlock>()
                .AnyAsync(b => (b.BlockerId == userId && b.BlockedUserId == targetUserId) ||
                               (b.BlockerId == targetUserId && b.BlockedUserId == userId), ct);
            if (isBlocked) return Result<ConversationDto>.Failure("Bu kullanıcı ile konuşamazsınız.");

            // Mevcut konuşmayı kontrol et
            var existing = await _uow.Conversations.GetPrivateConversationAsync(userId, targetUserId, ct);
            if (existing != null)
                return Result<ConversationDto>.Success(await MapConversationAsync(existing, userId, ct));

            var targetUser = await _uow.Users.GetByIdAsync(targetUserId, ct);
            if (targetUser == null) return Result<ConversationDto>.NotFound("Kullanıcı bulunamadı.");

            var conversation = new Conversation
            {
                Type = ConversationType.Private,
                CreatedByUserId = userId,
                Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId, Role = ParticipantRole.Member },
                new() { UserId = targetUserId, Role = ParticipantRole.Member }
            }
            };

            await _uow.Conversations.AddAsync(conversation, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.Conversations.GetByIdAsync(conversation.Id, ct);
            return Result<ConversationDto>.Success(await MapConversationAsync(created!, userId, ct), null, 201);
        }

        public async Task<Result<ConversationDto>> CreateGroupConversationAsync(Guid creatorId, CreateGroupConversationDto request, CancellationToken ct = default)
        {
            if (request.ParticipantIds.Count == 0)
                return Result<ConversationDto>.Failure("En az 1 katılımcı gereklidir.");

            var participants = new List<ConversationParticipant>
        {
            new() { UserId = creatorId, Role = ParticipantRole.Owner }
        };

            foreach (var pid in request.ParticipantIds.Distinct())
            {
                if (pid == creatorId) continue;
                var user = await _uow.Users.GetByIdAsync(pid, ct);
                if (user != null)
                    participants.Add(new ConversationParticipant { UserId = pid, Role = ParticipantRole.Member });
            }

            var conversation = new Conversation
            {
                Type = ConversationType.Group,
                Name = request.Name,
                Description = request.Description,
                IsPublic = request.IsPublic,
                MaxParticipants = request.MaxParticipants,
                CreatedByUserId = creatorId,
                Participants = participants
            };

            await _uow.Conversations.AddAsync(conversation, ct);
            await _uow.SaveChangesAsync(ct);

            return Result<ConversationDto>.Success(await MapConversationAsync(conversation, creatorId, ct), null, 201);
        }

        public async Task<Result<IEnumerable<ConversationDto>>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
        {
            var conversations = await _uow.Conversations.GetUserConversationsAsync(userId, ct);
            var dtos = new List<ConversationDto>();

            foreach (var conv in conversations)
                dtos.Add(await MapConversationAsync(conv, userId, ct));

            return Result<IEnumerable<ConversationDto>>.Success(dtos);
        }

        public async Task<Result<ConversationDto>> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            if (!await _uow.Conversations.IsUserParticipantAsync(conversationId, userId, ct))
                return Result<ConversationDto>.Forbidden("Bu konuşmaya erişim yetkiniz yok.");

            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result<ConversationDto>.NotFound();

            return Result<ConversationDto>.Success(await MapConversationAsync(conv, userId, ct));
        }

        public async Task<Result<PagedResult<MessageDto>>> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            if (!await _uow.Conversations.IsUserParticipantAsync(conversationId, userId, ct))
                return Result<PagedResult<MessageDto>>.Forbidden();

            var messages = await _uow.Messages.GetConversationMessagesAsync(conversationId, page, pageSize, ct);
            var totalCount = await _uow.Messages.CountAsync(m => m.ConversationId == conversationId, ct);

            var dtos = _mapper.Map<IEnumerable<MessageDto>>(messages);

            // Okundu olarak işaretle
            await _uow.Messages.MarkAsReadAsync(conversationId, userId, ct);
            await _uow.SaveChangesAsync(ct);

            return Result<PagedResult<MessageDto>>.Success(new PagedResult<MessageDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        public async Task<Result> UpdateGroupAsync(Guid conversationId, Guid userId, UpdateGroupDto request, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null || participant.HasLeft) return Result.Failure("Katılımcı bulunamadı.", 404);
            if (participant.Role == ParticipantRole.Member) return Result.Failure("Bu işlem için admin yetkisi gereklidir.", 403);

            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null || conv.Type == ConversationType.Private) return Result.Failure("Grup bulunamadı.", 404);

            if (request.Name != null) conv.Name = request.Name;
            if (request.Description != null) conv.Description = request.Description;
            if (request.OnlyAdminsCanMessage.HasValue) conv.OnlyAdminsCanMessage = request.OnlyAdminsCanMessage.Value;
            if (request.MaxParticipants.HasValue) conv.MaxParticipants = request.MaxParticipants.Value;

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Grup güncellendi.");
        }

        public async Task<Result<string>> UploadGroupAvatarAsync(Guid conversationId, Guid userId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null || participant.Role == ParticipantRole.Member)
                return Result<string>.Forbidden("Admin yetkisi gereklidir.");

            if (!_fileService.IsValidFileExtension(file.FileName))
                return Result<string>.Failure("Geçersiz dosya türü.");

            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result<string>.NotFound();

            if (!string.IsNullOrEmpty(conv.AvatarUrl))
                await _fileService.DeleteFileAsync(conv.AvatarUrl, ct);

            var url = await _fileService.UploadFileAsync(file, "groups", ct);
            conv.AvatarUrl = url;
            await _uow.SaveChangesAsync(ct);

            return Result<string>.Success(url, "Grup avatarı güncellendi.");
        }

        public async Task<Result> AddParticipantAsync(Guid conversationId, Guid userId, Guid targetUserId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null || participant.HasLeft || participant.Role == ParticipantRole.Member)
                return Result.Failure("Bu işlem için admin yetkisi gereklidir.", 403);

            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result.Failure("Konuşma bulunamadı.", 404);

            if (conv.MaxParticipants.HasValue)
            {
                var currentCount = await _uow.Repository<ConversationParticipant>()
                    .CountAsync(p => p.ConversationId == conversationId && !p.HasLeft, ct);
                if (currentCount >= conv.MaxParticipants.Value)
                    return Result.Failure("Grup maksimum katılımcı sayısına ulaşmıştır.");
            }

            var existingParticipant = await _uow.Conversations.GetParticipantAsync(conversationId, targetUserId, ct);
            if (existingParticipant != null)
            {
                if (!existingParticipant.HasLeft) return Result.Failure("Kullanıcı zaten grupta.");
                existingParticipant.HasLeft = false;
                existingParticipant.LeftAt = null;
                existingParticipant.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                await _uow.Repository<ConversationParticipant>().AddAsync(new ConversationParticipant
                {
                    ConversationId = conversationId,
                    UserId = targetUserId,
                    Role = ParticipantRole.Member
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Kullanıcı gruba eklendi.");
        }

        public async Task<Result> RemoveParticipantAsync(Guid conversationId, Guid userId, Guid targetUserId, CancellationToken ct = default)
        {
            var requestor = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (requestor == null || requestor.HasLeft || requestor.Role == ParticipantRole.Member)
                return Result.Failure("Bu işlem için admin yetkisi gereklidir.", 403);

            var target = await _uow.Conversations.GetParticipantAsync(conversationId, targetUserId, ct);
            if (target == null || target.HasLeft) return Result.Failure("Kullanıcı grupta değil.", 404);

            if (target.Role == ParticipantRole.Owner && requestor.Role != ParticipantRole.Owner)
                return Result.Failure("Grup sahibini çıkaramazsınız.", 403);

            target.HasLeft = true;
            target.LeftAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Kullanıcı gruptan çıkarıldı.");
        }

        public async Task<Result> LeaveGroupAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null || participant.HasLeft) return Result.Failure("Bu grupta değilsiniz.", 404);

            if (participant.Role == ParticipantRole.Owner)
            {
                // Sahipliği başkasına devret
                var nextAdmin = await _uow.Repository<ConversationParticipant>()
                    .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId != userId && !p.HasLeft && p.Role == ParticipantRole.Admin, ct);

                nextAdmin ??= await _uow.Repository<ConversationParticipant>()
                    .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId != userId && !p.HasLeft, ct);

                if (nextAdmin != null)
                    nextAdmin.Role = ParticipantRole.Owner;
                else
                {
                    // Son kişiydi, grubu kapat
                    var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
                    if (conv != null) { conv.IsActive = false; conv.IsDeleted = true; }
                }
            }

            participant.HasLeft = true;
            participant.LeftAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Gruptan ayrıldınız.");
        }

        public async Task<Result> UpdateParticipantRoleAsync(Guid conversationId, Guid userId, Guid targetUserId, ParticipantRole role, CancellationToken ct = default)
        {
            var requestor = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (requestor == null || requestor.Role != ParticipantRole.Owner)
                return Result.Failure("Sadece grup sahibi rol değiştirebilir.", 403);

            var target = await _uow.Conversations.GetParticipantAsync(conversationId, targetUserId, ct);
            if (target == null || target.HasLeft) return Result.Failure("Kullanıcı grupta değil.", 404);

            target.Role = role;
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Rol güncellendi.");
        }

        public async Task<Result> MuteConversationAsync(Guid conversationId, Guid userId, DateTime? until, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null) return Result.Failure("Katılımcı bulunamadı.", 404);

            participant.IsMuted = true;
            participant.MutedUntil = until;
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Konuşma sessize alındı.");
        }

        public async Task<Result> UnmuteConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null) return Result.Failure("Katılımcı bulunamadı.", 404);

            participant.IsMuted = false;
            participant.MutedUntil = null;
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Konuşma sesi açıldı.");
        }

        public async Task<Result> ArchiveConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null) return Result.Failure("Katılımcı bulunamadı.", 404);

            participant.IsArchived = !participant.IsArchived;
            await _uow.SaveChangesAsync(ct);
            return Result.Success(participant.IsArchived ? "Arşivlendi." : "Arşivden çıkarıldı.");
        }

        public async Task<Result> PinConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null) return Result.Failure("Katılımcı bulunamadı.", 404);

            participant.IsPinned = !participant.IsPinned;
            await _uow.SaveChangesAsync(ct);
            return Result.Success(participant.IsPinned ? "Sabitlendsi." : "Sabitlemesi kaldırıldı.");
        }

        public async Task<Result> DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result.Failure("Konuşma bulunamadı.", 404);

            if (conv.Type == ConversationType.Private)
            {
                var isParticipant = await _uow.Conversations.IsUserParticipantAsync(conversationId, userId, ct);
                if (!isParticipant) return Result.Forbidden();
            }
            else
            {
                var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
                if (participant?.Role != ParticipantRole.Owner) return Result.Forbidden("Sadece grup sahibi grubu silebilir.");
            }

            await _uow.Conversations.SoftDeleteAsync(conv, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Konuşma silindi.");
        }

        public async Task<Result<string>> GenerateInviteLinkAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            var participant = await _uow.Conversations.GetParticipantAsync(conversationId, userId, ct);
            if (participant == null || participant.Role == ParticipantRole.Member)
                return Result<string>.Forbidden("Admin yetkisi gereklidir.");

            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result<string>.NotFound();

            conv.InviteLink = Guid.NewGuid().ToString("N");
            await _uow.SaveChangesAsync(ct);

            return Result<string>.Success(conv.InviteLink, "Davet linki oluşturuldu.");
        }

        public async Task<Result<ConversationDto>> JoinByInviteLinkAsync(string inviteLink, Guid userId, CancellationToken ct = default)
        {
            var conv = await _uow.Conversations.FirstOrDefaultAsync(c => c.InviteLink == inviteLink, ct);
            if (conv == null) return Result<ConversationDto>.NotFound("Geçersiz davet linki.");
            if (!conv.IsPublic && conv.Type == ConversationType.Group)
            {
                // Özel grup - sadece link ile girilir
            }

            var existing = await _uow.Conversations.GetParticipantAsync(conv.Id, userId, ct);
            if (existing != null && !existing.HasLeft)
                return Result<ConversationDto>.Failure("Zaten bu grubun üyesisiniz.");

            if (existing != null)
            {
                existing.HasLeft = false;
                existing.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                await _uow.Repository<ConversationParticipant>().AddAsync(new ConversationParticipant
                {
                    ConversationId = conv.Id,
                    UserId = userId,
                    Role = ParticipantRole.Member
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Result<ConversationDto>.Success(await MapConversationAsync(conv, userId, ct));
        }

        private async Task<ConversationDto> MapConversationAsync(Conversation conv, Guid userId, CancellationToken ct)
        {
            var dto = _mapper.Map<ConversationDto>(conv);

            var participant = await _uow.Conversations.GetParticipantAsync(conv.Id, userId, ct);
            if (participant != null)
            {
                dto.IsMuted = participant.IsMuted;
                dto.IsArchived = participant.IsArchived;
                dto.IsPinned = participant.IsPinned;
                dto.UnreadCount = await _uow.Messages.GetUnreadCountAsync(conv.Id, userId, ct);
            }

            if (conv.LastMessageId.HasValue)
            {
                var lastMsg = await _uow.Messages.GetByIdAsync(conv.LastMessageId.Value, ct);
                if (lastMsg != null) dto.LastMessage = _mapper.Map<MessageDto>(lastMsg);
            }

            dto.LastActivityAt = conv.LastMessage?.CreatedAt ?? conv.CreatedAt;
            return dto;
        }
    }
}
