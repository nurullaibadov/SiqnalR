using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Features.Auth.DTOs;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Yeni kullanıcı kaydı</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(request, ct);
            return HandleResult(result);
        }

        /// <summary>Kullanıcı girişi</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);
            return HandleResult(result);
        }

        /// <summary>Access token yenile</summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
            return HandleResult(result);
        }

        /// <summary>Çıkış yap</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var result = await _authService.LogoutAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>Email doğrulama</summary>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
        {
            var result = await _authService.VerifyEmailAsync(token, ct);
            return HandleResult(result);
        }

        /// <summary>Şifre sıfırlama maili gönder</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request, CancellationToken ct)
        {
            var result = await _authService.ForgotPasswordAsync(request.Email, ct);
            return HandleResult(result);
        }

        /// <summary>Şifre sıfırla</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
        {
            var result = await _authService.ResetPasswordAsync(request, ct);
            return HandleResult(result);
        }

        /// <summary>Şifre değiştir (giriş gerekli)</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken ct)
        {
            var result = await _authService.ChangePasswordAsync(CurrentUserId, request, ct);
            return HandleResult(result);
        }

        /// <summary>2FA ile giriş</summary>
        [HttpPost("two-factor-login")]
        [AllowAnonymous]
        public async Task<IActionResult> TwoFactorLogin([FromBody] TwoFactorLoginDto request, CancellationToken ct)
        {
            var result = await _authService.LoginWithTwoFactorAsync(request, ct);
            return HandleResult(result);
        }

        /// <summary>2FA aktifleştir</summary>
        [HttpPost("enable-two-factor")]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor(CancellationToken ct)
        {
            var result = await _authService.EnableTwoFactorAsync(CurrentUserId, ct);
            return HandleResult(result);
        }

        /// <summary>2FA deaktifleştir</summary>
        [HttpPost("disable-two-factor")]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorDto request, CancellationToken ct)
        {
            var result = await _authService.DisableTwoFactorAsync(CurrentUserId, request.Code, ct);
            return HandleResult(result);
        }
    }

    // Ekstra DTO'lar
    public record ForgotPasswordDto(string Email);
    public record DisableTwoFactorDto(string Code);
}
