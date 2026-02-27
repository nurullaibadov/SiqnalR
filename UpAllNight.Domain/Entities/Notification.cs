using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string? Data { get; set; } // JSON payload
        public string? ImageUrl { get; set; }
        public string? ActionUrl { get; set; }
    }

    public enum NotificationType
    {
        Message = 0,
        GroupInvite = 1,
        ContactRequest = 2,
        SystemAlert = 3,
        Mention = 4
    }
}
