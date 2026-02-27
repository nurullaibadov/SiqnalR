using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Features.Users.DTOs;
using UpAllNight.Application.Features.Users.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Kendi profilini getir</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var result = await _userService.GetProfileAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı profilini getir</summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
        {
            var result = await _userService.GetProfileAsync(userId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı adına göre profil getir</summary>
        [HttpGet("by-username/{userName}")]
        public async Task<IActionResult> GetProfileByUserName(string userName, CancellationToken ct)
        {
            var result = await _userService.GetProfileByUserNameAsync(userName, ct);
            return HandleResult(result);
        }

        /// <summary>Profil güncelle</summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken ct)
        {
            var result = await _userService.UpdateProfileAsync(CurrentUserId, request, ct);
            return HandleResult(result);
        }

        /// <summary>Profil fotoğrafı yükle</summary>
        [HttpPost("me/profile-picture")]
        [RequestSizeLimit(5_242_880)] // 5MB
        public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
        {
            var result = await _userService.UploadProfilePictureAsync(CurrentUserId, file, ct);
            return HandleResult(result);
        }

        /// <summary>Profil fotoğrafını sil</summary>
        [HttpDelete("me/profile-picture")]
        public async Task<IActionResult> DeleteProfilePicture(CancellationToken ct)
        {
            var result = await _userService.DeleteProfilePictureAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı ara</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest(new { success = false, message = "En az 2 karakter girin." });

            var result = await _userService.SearchUsersAsync(q, page, pageSize, ct);
            return HandleResult(result);
        }

        // ---- CONTACTS ----

        /// <summary>Rehber listesi</summary>
        [HttpGet("me/contacts")]
        public async Task<IActionResult> GetContacts(CancellationToken ct)
        {
            var result = await _userService.GetContactsAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Rehbere kişi ekle</summary>
        [HttpPost("me/contacts/{contactId:guid}")]
        public async Task<IActionResult> AddContact(Guid contactId, [FromQuery] string? nickname, CancellationToken ct)
        {
            var result = await _userService.AddContactAsync(CurrentUserId, contactId, nickname, ct);
            return HandleResult(result);
        }

        /// <summary>Rehberden kişi sil</summary>
        [HttpDelete("me/contacts/{contactId:guid}")]
        public async Task<IActionResult> RemoveContact(Guid contactId, CancellationToken ct)
        {
            var result = await _userService.RemoveContactAsync(CurrentUserId, contactId, ct);
            return HandleResult(result);
        }

        // ---- BLOCK ----

        /// <summary>Engellenen kullanıcılar</summary>
        [HttpGet("me/blocked")]
        public async Task<IActionResult> GetBlockedUsers(CancellationToken ct)
        {
            var result = await _userService.GetBlockedUsersAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı engelle</summary>
        [HttpPost("me/blocked/{targetId:guid}")]
        public async Task<IActionResult> BlockUser(Guid targetId, [FromQuery] string? reason, CancellationToken ct)
        {
            var result = await _userService.BlockUserAsync(CurrentUserId, targetId, reason, ct);
            return HandleResult(result);
        }

        /// <summary>Engeli kaldır</summary>
        [HttpDelete("me/blocked/{targetId:guid}")]
        public async Task<IActionResult> UnblockUser(Guid targetId, CancellationToken ct)
        {
            var result = await _userService.UnblockUserAsync(CurrentUserId, targetId, ct);
            return HandleResult(result);
        }
    }
}
