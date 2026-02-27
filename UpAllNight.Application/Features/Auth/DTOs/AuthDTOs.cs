using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Features.Auth.DTOs
{
    public record RegisterRequestDto(
       [Required][StringLength(50, MinimumLength = 3)] string UserName,
       [Required][EmailAddress] string Email,
       [Required][Phone] string PhoneNumber,
       [Required][StringLength(100, MinimumLength = 8)] string Password,
       [Required][Compare(nameof(Password))] string ConfirmPassword,
       string? FirstName,
       string? LastName
   );

    public record LoginRequestDto(
        [Required] string EmailOrUserName,
        [Required] string Password,
        bool RememberMe = false
    );

    public record ResetPasswordRequestDto(
        [Required] string Token,
        [Required][EmailAddress] string Email,
        [Required][StringLength(100, MinimumLength = 8)] string NewPassword,
        [Required][Compare(nameof(NewPassword))] string ConfirmPassword
    );

    public record ChangePasswordRequestDto(
        [Required] string CurrentPassword,
        [Required][StringLength(100, MinimumLength = 8)] string NewPassword,
        [Required][Compare(nameof(NewPassword))] string ConfirmPassword
    );

    public record TwoFactorLoginDto(
        [Required] string UserId,
        [Required] string Code
    );

    public class AuthResponseDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public bool RequiresTwoFactor { get; set; } = false;
    }

    public record RefreshTokenRequestDto([Required] string RefreshToken);
}
