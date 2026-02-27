using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["FromName"], emailSettings["FromEmail"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                emailSettings["Host"],
                int.Parse(emailSettings["Port"]!),
                SecureSocketOptions.StartTls,
                cancellationToken
            );
            await client.AuthenticateAsync(emailSettings["UserName"], emailSettings["Password"], cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        public async Task SendEmailVerificationAsync(string to, string name, string verificationLink, CancellationToken cancellationToken = default)
        {
            var body = $@"
        <html><body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #007bff;'>Email Doğrulama</h2>
                <p>Merhaba {name},</p>
                <p>Hesabınızı doğrulamak için aşağıdaki butona tıklayın:</p>
                <a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Email'imi Doğrula</a>
                <p style='margin-top: 20px; color: #666;'>Bu link 24 saat geçerlidir.</p>
                <p>Eğer bu hesabı siz oluşturmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
            </div>
        </body></html>";

            await SendEmailAsync(to, "Email Adresinizi Doğrulayın", body, true, cancellationToken);
        }

        public async Task SendPasswordResetAsync(string to, string name, string resetLink, CancellationToken cancellationToken = default)
        {
            var body = $@"
        <html><body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #dc3545;'>Şifre Sıfırlama</h2>
                <p>Merhaba {name},</p>
                <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
                <a href='{resetLink}' style='background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Şifremi Sıfırla</a>
                <p style='margin-top: 20px; color: #666;'>Bu link 1 saat geçerlidir.</p>
                <p>Eğer şifre sıfırlama talebinde bulunmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
            </div>
        </body></html>";

            await SendEmailAsync(to, "Şifre Sıfırlama İsteği", body, true, cancellationToken);
        }

        public async Task SendWelcomeEmailAsync(string to, string name, CancellationToken cancellationToken = default)
        {
            var body = $@"
        <html><body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #28a745;'>Hoş Geldiniz! 🎉</h2>
                <p>Merhaba {name},</p>
                <p>MessagingApp ailesine hoş geldiniz!</p>
                <p>Artık arkadaşlarınızla mesajlaşmaya başlayabilirsiniz.</p>
            </div>
        </body></html>";

            await SendEmailAsync(to, "MessagingApp'e Hoş Geldiniz!", body, true, cancellationToken);
        }

        public async Task SendTwoFactorCodeAsync(string to, string name, string code, CancellationToken cancellationToken = default)
        {
            var body = $@"
        <html><body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #007bff;'>İki Faktörlü Doğrulama</h2>
                <p>Merhaba {name},</p>
                <p>Doğrulama kodunuz:</p>
                <div style='font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 8px; text-align: center; padding: 20px; background: #f8f9fa; border-radius: 8px;'>{code}</div>
                <p style='margin-top: 20px; color: #666;'>Bu kod 5 dakika geçerlidir.</p>
            </div>
        </body></html>";

            await SendEmailAsync(to, "Doğrulama Kodunuz", body, true, cancellationToken);
        }
    }
}
