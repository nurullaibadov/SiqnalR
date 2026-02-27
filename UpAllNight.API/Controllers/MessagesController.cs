using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Features.Messages.DTOs;
using UpAllNight.Application.Features.Messages.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class MessagesController : BaseController
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        /// <summary>Mesaj gönder (dosya + metin)</summary>
        [HttpPost]
        [RequestSizeLimit(52_428_800)] // 50MB
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequestDto request, CancellationToken ct)
        {
            var files = Request.Form.Files.Count > 0 ? Request.Form.Files : null;
            var result = await _messageService.SendMessageAsync(CurrentUserId, request, files, ct);
            return HandleResult(result);
        }

        /// <summary>Mesaj düzenle</summary>
        [HttpPut("{messageId:guid}")]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequestDto request, CancellationToken ct)
        {
            var result = await _messageService.EditMessageAsync(CurrentUserId, messageId, request, ct);
            return HandleResult(result);
        }

        /// <summary>Mesaj sil</summary>
        [HttpDelete("{messageId:guid}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId, [FromQuery] bool deleteForEveryone = false, CancellationToken ct = default)
        {
            var result = await _messageService.DeleteMessageAsync(CurrentUserId, messageId, deleteForEveryone, ct);
            return HandleResult(result);
        }

        /// <summary>Mesaj ilet</summary>
        [HttpPost("forward")]
        public async Task<IActionResult> ForwardMessage([FromBody] ForwardMessageRequestDto request, CancellationToken ct)
        {
            var result = await _messageService.ForwardMessageAsync(CurrentUserId, request, ct);
            return HandleResult(result);
        }

        /// <summary>Mesaja reaksiyon ekle</summary>
        [HttpPost("{messageId:guid}/reactions")]
        public async Task<IActionResult> AddReaction(Guid messageId, [FromBody] AddReactionDto request, CancellationToken ct)
        {
            var result = await _messageService.ReactToMessageAsync(CurrentUserId, messageId, request.Emoji, ct);
            return HandleResult(result);
        }

        /// <summary>Reaksiyonu kaldır</summary>
        [HttpDelete("{messageId:guid}/reactions")]
        public async Task<IActionResult> RemoveReaction(Guid messageId, CancellationToken ct)
        {
            var result = await _messageService.RemoveReactionAsync(CurrentUserId, messageId, ct);
            return HandleResult(result);
        }

        /// <summary>Mesajları okundu işaretle</summary>
        [HttpPost("conversations/{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken ct)
        {
            var result = await _messageService.MarkAsReadAsync(conversationId, CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Mesajlarda ara</summary>
        [HttpGet("conversations/{conversationId:guid}/search")]
        public async Task<IActionResult> SearchMessages(Guid conversationId, [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var result = await _messageService.SearchMessagesAsync(conversationId, CurrentUserId, q, page, pageSize, ct);
            return HandleResult(result);
        }
    }

    public record AddReactionDto(string Emoji);
}
