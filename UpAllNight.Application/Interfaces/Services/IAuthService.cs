using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Auth.DTOs;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);
        Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
        Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> LoginWithTwoFactorAsync(TwoFactorLoginDto request, CancellationToken cancellationToken = default);
        Task<Result> EnableTwoFactorAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result> DisableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    }
}
