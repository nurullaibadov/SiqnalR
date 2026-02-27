using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Admin.Services;
using UpAllNight.Application.Features.Users.DTOs;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Dashboard istatistikleri</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var result = await _adminService.GetDashboardStatsAsync(ct);
            return HandleResult(result);
        }

        // ---- USERS ----

        /// <summary>Tüm kullanıcıları listele</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _adminService.GetAllUsersAsync(request, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı detayı</summary>
        [HttpGet("users/{userId:guid}")]
        public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.GetUserByIdAsync(userId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı rolü güncelle</summary>
        [HttpPut("users/{userId:guid}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleDto request, CancellationToken ct)
        {
            var result = await _adminService.UpdateUserRoleAsync(CurrentUserId, userId, request.Role, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı yasakla</summary>
        [HttpPost("users/{userId:guid}/ban")]
        public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserDto request, CancellationToken ct)
        {
            var result = await _adminService.BanUserAsync(CurrentUserId, userId, request.Reason, request.BannedUntil, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı yasağı kaldır</summary>
        [HttpPost("users/{userId:guid}/unban")]
        public async Task<IActionResult> UnbanUser(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.UnbanUserAsync(CurrentUserId, userId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı deaktif et</summary>
        [HttpPost("users/{userId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.DeactivateUserAsync(CurrentUserId, userId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı aktif et</summary>
        [HttpPost("users/{userId:guid}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.ActivateUserAsync(CurrentUserId, userId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı sil</summary>
        [HttpDelete("users/{userId:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.DeleteUserAsync(CurrentUserId, userId, ct);
            return HandleResult(result);
        }

        // ---- CONVERSATIONS ----

        /// <summary>Konuşma sil (Admin)</summary>
        [HttpDelete("conversations/{conversationId:guid}")]
        public async Task<IActionResult> DeleteConversation(Guid conversationId, CancellationToken ct)
        {
            var result = await _adminService.DeleteConversationAsync(CurrentUserId, conversationId, ct);
            return HandleResult(result);
        }

        // ---- MESSAGES ----

        /// <summary>Mesaj sil (Admin)</summary>
        [HttpDelete("messages/{messageId:guid}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId, CancellationToken ct)
        {
            var result = await _adminService.DeleteMessageAsync(CurrentUserId, messageId, ct);
            return HandleResult(result);
        }

        // ---- AUDIT LOGS ----

        /// <summary>Audit loglarını getir</summary>
        [HttpGet("audit-logs")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _adminService.GetAuditLogsAsync(request, ct);
            return HandleResult(result);
        }
    }
}
