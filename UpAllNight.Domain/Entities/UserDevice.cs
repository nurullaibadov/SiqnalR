using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class UserDevice : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string DeviceToken { get; set; } = null!;
        public DevicePlatform Platform { get; set; }
        public string? DeviceName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }

    public enum DevicePlatform
    {
        Android = 0,
        iOS = 1,
        Web = 2,
        Windows = 3,
        MacOS = 4
    }
}
