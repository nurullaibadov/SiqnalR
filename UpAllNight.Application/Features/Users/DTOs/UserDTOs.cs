using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Features.Users.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName => $"{FirstName} {LastName}".Trim();
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserProfileDto : UserDto
    {
        public bool IsTwoFactorEnabled { get; set; }
        public bool IsPhoneVerified { get; set; }
        public int ContactsCount { get; set; }
        public int GroupsCount { get; set; }
    }

    public record UpdateProfileRequestDto(
        string? FirstName,
        string? LastName,
        string? Bio,
        string? Status,
        [Phone] string? PhoneNumber
    );

    public record UpdateUserRoleDto(
        [Required] Guid UserId,
        [Required] UserRole Role
    );

    public record BanUserDto(
        [Required] Guid UserId,
        string? Reason,
        DateTime? BannedUntil
    );
}
