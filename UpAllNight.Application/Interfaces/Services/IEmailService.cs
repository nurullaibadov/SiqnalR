using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
        Task SendEmailVerificationAsync(string to, string name, string verificationLink, CancellationToken cancellationToken = default);
        Task SendPasswordResetAsync(string to, string name, string resetLink, CancellationToken cancellationToken = default);
        Task SendWelcomeEmailAsync(string to, string name, CancellationToken cancellationToken = default);
        Task SendTwoFactorCodeAsync(string to, string name, string code, CancellationToken cancellationToken = default);
    }
}
