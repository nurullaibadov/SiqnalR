using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class User : BaseAuditableEntity
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; } = "Hey there! I am using MessagingApp";
        public bool IsOnline { get; set; } = false;
        public DateTime? LastSeenAt { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsPhoneVerified { get; set; } = false;
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public bool IsActive { get; set; } = true;
        public bool IsBanned { get; set; } = false;
        public string? BanReason { get; set; }
        public DateTime? BannedUntil { get; set; }

        // Tokens
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Navigation Properties
        public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<UserContact> Contacts { get; set; } = new List<UserContact>();
        public ICollection<UserContact> ContactOf { get; set; } = new List<UserContact>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
        public ICollection<UserBlock> BlockedBy { get; set; } = new List<UserBlock>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    }

    public enum UserRole
    {
        User = 0,
        Moderator = 1,
        Admin = 2,
        SuperAdmin = 3
    }
}
