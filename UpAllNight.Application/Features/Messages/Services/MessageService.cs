using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Messages.DTOs;
using UpAllNight.Application.Interfaces.Services;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Messages.Services
{
    public interface IMessageService
    {
        Task<Result<MessageDto>> SendMessageAsync(Guid senderId, SendMessageRequestDto request, IFormFileCollection? files, CancellationToken ct = default);
        Task<Result<MessageDto>> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequestDto request, CancellationToken ct = default);
        Task<Result> DeleteMessageAsync(Guid userId, Guid messageId, bool deleteForEveryone, CancellationToken ct = default);
        Task<Result<MessageDto>> ForwardMessageAsync(Guid userId, ForwardMessageRequestDto request, CancellationToken ct = default);
        Task<Result> ReactToMessageAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default);
        Task<Result> RemoveReactionAsync(Guid userId, Guid messageId, CancellationToken ct = default);
        Task<Result> MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<Result<PagedResult<MessageDto>>> SearchMessagesAsync(Guid conversationId, Guid userId, string searchTerm, int page, int pageSize, CancellationToken ct = default);
    }

    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IHubContext<ChatHub> _chatHub;

        public MessageService(IUnitOfWork uow, IMapper mapper, IFileService fileService, IHubContext<ChatHub> chatHub)
        {
            _uow = uow;
            _mapper = mapper;
            _fileService = fileService;
            _chatHub = chatHub;
        }

        public async Task<Result<MessageDto>> SendMessageAsync(Guid senderId, SendMessageRequestDto request, IFormFileCollection? files, CancellationToken ct = default)
        {
            if (!await _uow.Conversations.IsUserParticipantAsync(request.ConversationId, senderId, ct))
                return Result<MessageDto>.Forbidden("Bu konuşmada mesaj gönderme yetkiniz yok.");

            // Engel kontrolü (private conversation için)
            var conv = await _uow.Conversations.GetByIdAsync(request.ConversationId, ct);
            if (conv == null) return Result<MessageDto>.NotFound("Konuşma bulunamadı.");

            if (conv.OnlyAdminsCanMessage)
            {
                var participant = await _uow.Conversations.GetParticipantAsync(request.ConversationId, senderId, ct);
                if (participant?.Role == ParticipantRole.Member)
                    return Result<MessageDto>.Forbidden("Bu grupta sadece adminler mesaj gönderebilir.");
            }

            var message = new Message
            {
                ConversationId = request.ConversationId,
                SenderId = senderId,
                Content = request.Content,
                Type = request.Type,
                ReplyToMessageId = request.ReplyToMessageId,
                Status = MessageStatus.Sent
            };

            await _uow.Messages.AddAsync(message, ct);

            // Dosya ekleri
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (!_fileService.IsValidFileExtension(file.FileName)) continue;
                    if (!_fileService.IsValidFileSize(file.Length)) continue;

                    var folder = GetFolderForFile(file.ContentType);
                    var url = await _fileService.UploadFileAsync(file, folder, ct);

                    var attachment = new MessageAttachment
                    {
                        MessageId = message.Id,
                        FileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}",
                        OriginalFileName = file.FileName,
                        FileUrl = url,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        Type = GetAttachmentType(file.ContentType)
                    };
                    await _uow.Repository<MessageAttachment>().AddAsync(attachment, ct);
                }

                if (request.Type == MessageType.Text && files.Count > 0)
                    message.Type = GetMessageTypeFromFile(files[0].ContentType);
            }

            // Son mesajı güncelle
            conv.LastMessageId = message.Id;
            await _uow.SaveChangesAsync(ct);

            // Mesajı tam yükle
            var savedMessage = await _uow.Messages.GetByIdAsync(message.Id, ct);

            // SignalR ile gönder
            await _chatHub.Clients
                .Group($"conversation_{request.ConversationId}")
                .SendAsync("NewMessage", _mapper.Map<MessageDto>(message), ct);

            return Result<MessageDto>.Success(_mapper.Map<MessageDto>(savedMessage ?? message), null, 201);
        }

        public async Task<Result<MessageDto>> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequestDto request, CancellationToken ct = default)
        {
            var message = await _uow.Messages.GetByIdAsync(messageId, ct);
            if (message == null) return Result<MessageDto>.NotFound("Mesaj bulunamadı.");
            if (message.SenderId != userId) return Result<MessageDto>.Forbidden("Başkasının mesajını düzenleyemezsiniz.");
            if (message.Type != MessageType.Text) return Result<MessageDto>.Failure("Sadece metin mesajları düzenlenebilir.");

            // 15 dakika sınırı
            if ((DateTime.UtcNow - message.CreatedAt).TotalMinutes > 15)
                return Result<MessageDto>.Failure("Mesaj düzenleme süresi dolmuştur (15 dakika).");

            message.OriginalContent = message.Content;
            message.Content = request.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);

            var dto = _mapper.Map<MessageDto>(message);
            await _chatHub.Clients
                .Group($"conversation_{message.ConversationId}")
                .SendAsync("MessageEdited", dto, ct);

            return Result<MessageDto>.Success(dto, "Mesaj düzenlendi.");
        }

        public async Task<Result> DeleteMessageAsync(Guid userId, Guid messageId, bool deleteForEveryone, CancellationToken ct = default)
        {
            var message = await _uow.Messages.GetByIdAsync(messageId, ct);
            if (message == null) return Result.Failure("Mesaj bulunamadı.", 404);

            if (deleteForEveryone)
            {
                if (message.SenderId != userId)
                {
                    // Admin kontrolü
                    var participant = await _uow.Conversations.GetParticipantAsync(message.ConversationId, userId, ct);
                    if (participant?.Role == ParticipantRole.Member)
                        return Result.Failure("Başkasının mesajını silemezsiniz.", 403);
                }

                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;
                message.Content = null;

                await _uow.SaveChangesAsync(ct);

                await _chatHub.Clients
                    .Group($"conversation_{message.ConversationId}")
                    .SendAsync("MessageDeleted", new { MessageId = messageId, DeletedForEveryone = true }, ct);
            }
            else
            {
                // Sadece kendin için sil (soft delete with user tracking - simplified)
                if (message.SenderId != userId) return Result.Failure("Sadece kendi mesajlarınızı silebilirsiniz.", 403);
                message.IsDeleted = true;
                await _uow.SaveChangesAsync(ct);
            }

            return Result.Success("Mesaj silindi.");
        }

        public async Task<Result<MessageDto>> ForwardMessageAsync(Guid userId, ForwardMessageRequestDto request, CancellationToken ct = default)
        {
            var original = await _uow.Messages.GetByIdAsync(request.MessageId, ct);
            if (original == null) return Result<MessageDto>.NotFound("Orijinal mesaj bulunamadı.");

            if (!await _uow.Conversations.IsUserParticipantAsync(request.TargetConversationId, userId, ct))
                return Result<MessageDto>.Forbidden("Hedef konuşmaya erişim yetkiniz yok.");

            var forwardedMessage = new Message
            {
                ConversationId = request.TargetConversationId,
                SenderId = userId,
                Content = original.Content,
                Type = original.Type,
                IsForwarded = true,
                OriginalMessageId = original.Id,
                Status = MessageStatus.Sent
            };

            await _uow.Messages.AddAsync(forwardedMessage, ct);

            // Ekleri kopyala
            foreach (var att in original.Attachments ?? new List<MessageAttachment>())
            {
                await _uow.Repository<MessageAttachment>().AddAsync(new MessageAttachment
                {
                    MessageId = forwardedMessage.Id,
                    FileName = att.FileName,
                    OriginalFileName = att.OriginalFileName,
                    FileUrl = att.FileUrl,
                    ContentType = att.ContentType,
                    FileSize = att.FileSize,
                    Type = att.Type,
                    ThumbnailUrl = att.ThumbnailUrl
                }, ct);
            }

            var conv = await _uow.Conversations.GetByIdAsync(request.TargetConversationId, ct);
            if (conv != null) conv.LastMessageId = forwardedMessage.Id;

            await _uow.SaveChangesAsync(ct);

            var dto = _mapper.Map<MessageDto>(forwardedMessage);
            await _chatHub.Clients
                .Group($"conversation_{request.TargetConversationId}")
                .SendAsync("NewMessage", dto, ct);

            return Result<MessageDto>.Success(dto, null, 201);
        }

        public async Task<Result> ReactToMessageAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default)
        {
            var message = await _uow.Messages.GetByIdAsync(messageId, ct);
            if (message == null) return Result.Failure("Mesaj bulunamadı.", 404);

            // Mevcut reaksiyonu güncelle veya yeni ekle
            var existing = await _uow.Repository<MessageReaction>()
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId, ct);

            if (existing != null)
            {
                existing.Emoji = emoji;
            }
            else
            {
                await _uow.Repository<MessageReaction>().AddAsync(new MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    Emoji = emoji
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);

            await _chatHub.Clients
                .Group($"conversation_{message.ConversationId}")
                .SendAsync("MessageReaction", new { MessageId = messageId, UserId = userId, Emoji = emoji }, ct);

            return Result.Success("Reaksiyon eklendi.");
        }

        public async Task<Result> RemoveReactionAsync(Guid userId, Guid messageId, CancellationToken ct = default)
        {
            var reaction = await _uow.Repository<MessageReaction>()
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId, ct);

            if (reaction == null) return Result.Failure("Reaksiyon bulunamadı.", 404);

            var message = await _uow.Messages.GetByIdAsync(messageId, ct);

            await _uow.Repository<MessageReaction>().DeleteAsync(reaction, ct);
            await _uow.SaveChangesAsync(ct);

            if (message != null)
            {
                await _chatHub.Clients
                    .Group($"conversation_{message.ConversationId}")
                    .SendAsync("ReactionRemoved", new { MessageId = messageId, UserId = userId }, ct);
            }

            return Result.Success("Reaksiyon kaldırıldı.");
        }

        public async Task<Result> MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
        {
            await _uow.Messages.MarkAsReadAsync(conversationId, userId, ct);
            await _uow.SaveChangesAsync(ct);

            await _chatHub.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("MessagesRead", new { ConversationId = conversationId, UserId = userId }, ct);

            return Result.Success();
        }

        public async Task<Result<PagedResult<MessageDto>>> SearchMessagesAsync(Guid conversationId, Guid userId, string searchTerm, int page, int pageSize, CancellationToken ct = default)
        {
            if (!await _uow.Conversations.IsUserParticipantAsync(conversationId, userId, ct))
                return Result<PagedResult<MessageDto>>.Forbidden();

            var (items, total) = await _uow.Messages.GetPagedAsync(
                predicate: m => m.ConversationId == conversationId && m.Content != null && m.Content.Contains(searchTerm),
                orderBy: q => q.OrderByDescending(m => m.CreatedAt),
                pageNumber: page,
                pageSize: pageSize,
                cancellationToken: ct
            );

            return Result<PagedResult<MessageDto>>.Success(new PagedResult<MessageDto>
            {
                Items = _mapper.Map<IEnumerable<MessageDto>>(items),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        private static string GetFolderForFile(string contentType)
        {
            if (contentType.StartsWith("image/")) return "images";
            if (contentType.StartsWith("video/")) return "videos";
            if (contentType.StartsWith("audio/")) return "audios";
            return "documents";
        }

        private static AttachmentType GetAttachmentType(string contentType)
        {
            if (contentType.StartsWith("image/")) return AttachmentType.Image;
            if (contentType.StartsWith("video/")) return AttachmentType.Video;
            if (contentType.StartsWith("audio/")) return AttachmentType.Audio;
            return AttachmentType.Document;
        }

        private static MessageType GetMessageTypeFromFile(string contentType)
        {
            if (contentType.StartsWith("image/")) return MessageType.Image;
            if (contentType.StartsWith("video/")) return MessageType.Video;
            if (contentType.StartsWith("audio/")) return MessageType.Audio;
            return MessageType.Document;
        }
    }
}
