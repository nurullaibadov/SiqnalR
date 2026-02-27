using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Auth.DTOs;
using UpAllNight.Application.Interfaces.Services;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
        {
            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email, cancellationToken))
                return Result<AuthResponseDto>.Failure("Bu email adresi zaten kullanılıyor.");

            if (await _unitOfWork.Users.IsUserNameExistsAsync(request.UserName, cancellationToken))
                return Result<AuthResponseDto>.Failure("Bu kullanıcı adı zaten kullanılıyor.");

            if (await _unitOfWork.Users.IsPhoneNumberExistsAsync(request.PhoneNumber, cancellationToken))
                return Result<AuthResponseDto>.Failure("Bu telefon numarası zaten kullanılıyor.");

            var verificationToken = GenerateSecureToken();
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email.ToLower(),
                PhoneNumber = request.PhoneNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                Role = UserRole.User
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Email gönder
            var verificationLink = $"{_configuration["FileSettings:BaseUrl"]}/api/v1/auth/verify-email?token={verificationToken}";
            _ = _emailService.SendEmailVerificationAsync(user.Email, user.FirstName ?? user.UserName, verificationLink, cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!)),
                RefreshTokenExpiry = user.RefreshTokenExpiry.Value
            }, "Kayıt başarılı! Lütfen emailinizi doğrulayın.", 201);
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            User? user = null;

            if (request.EmailOrUserName.Contains('@'))
                user = await _unitOfWork.Users.GetByEmailAsync(request.EmailOrUserName, cancellationToken);
            else
                user = await _unitOfWork.Users.GetByUserNameAsync(request.EmailOrUserName, cancellationToken);

            if (user == null)
                return Result<AuthResponseDto>.Failure("Geçersiz kimlik bilgileri.", 401);

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Result<AuthResponseDto>.Failure("Geçersiz kimlik bilgileri.", 401);

            if (!user.IsActive)
                return Result<AuthResponseDto>.Failure("Hesabınız deaktif edilmiştir.", 403);

            if (user.IsBanned)
            {
                if (user.BannedUntil.HasValue && user.BannedUntil < DateTime.UtcNow)
                {
                    user.IsBanned = false;
                    user.BanReason = null;
                    user.BannedUntil = null;
                }
                else
                {
                    return Result<AuthResponseDto>.Failure($"Hesabınız yasaklanmıştır. Sebep: {user.BanReason}", 403);
                }
            }

            if (user.IsTwoFactorEnabled)
            {
                return Result<AuthResponseDto>.Success(new AuthResponseDto
                {
                    UserId = user.Id,
                    RequiresTwoFactor = true
                }, "İki faktörlü doğrulama gerekiyor.");
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(
                request.RememberMe ? 90 : int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!));
            user.IsOnline = true;
            user.LastSeenAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = user.Role.ToString(),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!)),
                RefreshTokenExpiry = user.RefreshTokenExpiry.Value
            }, "Giriş başarılı.");
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Result<AuthResponseDto>.Failure("Geçersiz veya süresi dolmuş refresh token.", 401);

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!)),
                RefreshTokenExpiry = user.RefreshTokenExpiry.Value
            });
        }

        public async Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.IsOnline = false;
            user.LastSeenAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("Çıkış başarılı.");
        }

        public async Task<Result> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(token, cancellationToken);

            if (user == null) return Result.Failure("Geçersiz doğrulama tokeni.", 400);
            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow) return Result.Failure("Doğrulama tokeni süresi dolmuştur.", 400);
            if (user.IsEmailVerified) return Result.Success("Email zaten doğrulanmış.");

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _ = _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName ?? user.UserName, cancellationToken);

            return Result.Success("Email başarıyla doğrulandı!");
        }

        public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null) return Result.Success("Eğer bu email kayıtlıysa, sıfırlama linki gönderildi."); // Security: don't reveal

            var resetToken = GenerateSecureToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resetLink = $"{_configuration["FileSettings:BaseUrl"]}/reset-password?token={resetToken}&email={email}";
            _ = _emailService.SendPasswordResetAsync(user.Email, user.FirstName ?? user.UserName, resetLink, cancellationToken);

            return Result.Success("Eğer bu email kayıtlıysa, sıfırlama linki gönderildi.");
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

            if (user == null || user.Email.ToLower() != request.Email.ToLower())
                return Result.Failure("Geçersiz token veya email.", 400);

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return Result.Failure("Şifre sıfırlama tokeni süresi dolmuştur.", 400);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.RefreshToken = null; // Tüm oturumları kapat

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("Şifreniz başarıyla sıfırlandı.");
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return Result.Failure("Mevcut şifre yanlış.", 400);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.RefreshToken = null;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("Şifreniz başarıyla değiştirildi.");
        }

        public async Task<Result<AuthResponseDto>> LoginWithTwoFactorAsync(TwoFactorLoginDto request, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return Result<AuthResponseDto>.Failure("Geçersiz kullanıcı.", 400);

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null) return Result<AuthResponseDto>.Failure("Kullanıcı bulunamadı.", 404);

            // TODO: TOTP validation with TwoFactorSecret
            // For now, email-based OTP check can be implemented here
            return Result<AuthResponseDto>.Failure("2FA henüz tam implemente edilmedi.", 501);
        }

        public Task<Result> EnableTwoFactorAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("2FA henüz implemente edilmedi.", 501));

        public Task<Result> DisableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Failure("2FA henüz implemente edilmedi.", 501));

        private static string GenerateSecureToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
