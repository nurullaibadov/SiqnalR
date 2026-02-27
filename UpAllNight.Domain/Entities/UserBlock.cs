using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class UserBlock : BaseEntity
    {
        public Guid BlockerId { get; set; }
        public User Blocker { get; set; } = null!;
        public Guid BlockedUserId { get; set; }
        public User BlockedUser { get; set; } = null!;
        public string? Reason { get; set; }
    }
}
