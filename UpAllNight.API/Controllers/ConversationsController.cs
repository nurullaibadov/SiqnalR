using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Features.Conversations.DTOs;
using UpAllNight.Application.Features.Conversations.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class ConversationsController : BaseController
    {
        private readonly IConversationService _conversationService;

        public ConversationsController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        /// <summary>Tüm konuşmaları listele</summary>
        [HttpGet]
        public async Task<IActionResult> GetConversations(CancellationToken ct)
        {
            var result = await _conversationService.GetUserConversationsAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Konuşma detayı</summary>
        [HttpGet("{conversationId:guid}")]
        public async Task<IActionResult> GetConversation(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.GetConversationAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Özel konuşma başlat</summary>
        [HttpPost("private")]
        public async Task<IActionResult> CreatePrivate([FromBody] CreatePrivateConversationDto request, CancellationToken ct)
        {
            var result = await _conversationService.CreatePrivateConversationAsync(CurrentUserId, request.TargetUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Grup oluştur</summary>
        [HttpPost("group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupConversationDto request, CancellationToken ct)
        {
            var result = await _conversationService.CreateGroupConversationAsync(CurrentUserId, request, ct);
            return HandleResult(result);
        }

        /// <summary>Mesajları getir (sayfalı)</summary>
        [HttpGet("{conversationId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        {
            var result = await _conversationService.GetMessagesAsync(conversationId, CurrentUserId, page, pageSize, ct);
            return HandleResult(result);
        }

        /// <summary>Grup güncelle</summary>
        [HttpPut("{conversationId:guid}")]
        public async Task<IActionResult> UpdateGroup(Guid conversationId, [FromBody] UpdateGroupDto request, CancellationToken ct)
        {
            var result = await _conversationService.UpdateGroupAsync(conversationId, CurrentUserId, request, ct);
            return HandleResult(result);
        }

        /// <summary>Grup avatarı yükle</summary>
        [HttpPost("{conversationId:guid}/avatar")]
        public async Task<IActionResult> UploadGroupAvatar(Guid conversationId, IFormFile file, CancellationToken ct)
        {
            var result = await _conversationService.UploadGroupAvatarAsync(conversationId, CurrentUserId, file, ct);
            return HandleResult(result);
        }

        /// <summary>Katılımcı ekle</summary>
        [HttpPost("{conversationId:guid}/participants/{targetUserId:guid}")]
        public async Task<IActionResult> AddParticipant(Guid conversationId, Guid targetUserId, CancellationToken ct)
        {
            var result = await _conversationService.AddParticipantAsync(conversationId, CurrentUserId, targetUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Katılımcı çıkar</summary>
        [HttpDelete("{conversationId:guid}/participants/{targetUserId:guid}")]
        public async Task<IActionResult> RemoveParticipant(Guid conversationId, Guid targetUserId, CancellationToken ct)
        {
            var result = await _conversationService.RemoveParticipantAsync(conversationId, CurrentUserId, targetUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Gruptan ayrıl</summary>
        [HttpPost("{conversationId:guid}/leave")]
        public async Task<IActionResult> LeaveGroup(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.LeaveGroupAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Katılımcı rolünü güncelle</summary>
        [HttpPut("{conversationId:guid}/participants/{targetUserId:guid}/role")]
        public async Task<IActionResult> UpdateParticipantRole(Guid conversationId, Guid targetUserId, [FromBody] UpdateParticipantRoleDto request, CancellationToken ct)
        {
            var result = await _conversationService.UpdateParticipantRoleAsync(conversationId, CurrentUserId, targetUserId, request.Role, ct);
            return HandleResult(result);
        }

        /// <summary>Sessize al</summary>
        [HttpPost("{conversationId:guid}/mute")]
        public async Task<IActionResult> Mute(Guid conversationId, [FromQuery] DateTime? until, CancellationToken ct)
        {
            var result = await _conversationService.MuteConversationAsync(conversationId, CurrentUserId, until, ct);
            return HandleResult(result);
        }

        /// <summary>Sesi aç</summary>
        [HttpPost("{conversationId:guid}/unmute")]
        public async Task<IActionResult> Unmute(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.UnmuteConversationAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Arşivle / Arşivden çıkar</summary>
        [HttpPost("{conversationId:guid}/archive")]
        public async Task<IActionResult> Archive(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.ArchiveConversationAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Sabitle / Sabitlemeyi kaldır</summary>
        [HttpPost("{conversationId:guid}/pin")]
        public async Task<IActionResult> Pin(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.PinConversationAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Konuşmayı sil</summary>
        [HttpDelete("{conversationId:guid}")]
        public async Task<IActionResult> Delete(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.DeleteConversationAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Davet linki oluştur</summary>
        [HttpPost("{conversationId:guid}/invite-link")]
        public async Task<IActionResult> GenerateInviteLink(Guid conversationId, CancellationToken ct)
        {
            var result = await _conversationService.GenerateInviteLinkAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Davet linki ile gruba katıl</summary>
        [HttpPost("join/{inviteLink}")]
        public async Task<IActionResult> JoinByInviteLink(string inviteLink, CancellationToken ct)
        {
            var result = await _conversationService.JoinByInviteLinkAsync(inviteLink, CurrentUserId, ct);
            return HandleResult(result);
        }
    }
}
