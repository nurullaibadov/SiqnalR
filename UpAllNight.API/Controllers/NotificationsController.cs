using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Features.Notifications.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationAppService _notificationService;

        public NotificationsController(INotificationAppService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>Bildirimleri listele</summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var result = await _notificationService.GetNotificationsAsync(CurrentUserId, page, pageSize, ct);
            return HandleResult(result);
        }

        /// <summary>Okunmamış bildirim sayısı</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var result = await _notificationService.GetUnreadCountAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Bildirimi okundu işaretle</summary>
        [HttpPut("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
        {
            var result = await _notificationService.MarkAsReadAsync(CurrentUserId, notificationId, ct);
            return HandleResult(result);
        }

        /// <summary>Tümünü okundu işaretle</summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var result = await _notificationService.MarkAllAsReadAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Bildirimi sil</summary>
        [HttpDelete("{notificationId:guid}")]
        public async Task<IActionResult> DeleteNotification(Guid notificationId, CancellationToken ct)
        {
            var result = await _notificationService.DeleteNotificationAsync(CurrentUserId, notificationId, ct);
            return HandleResult(result);
        }
    }
}
