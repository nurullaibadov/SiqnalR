using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class UserContact : BaseEntity
    {
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public Guid ContactUserId { get; set; }
        public User ContactUser { get; set; } = null!;
        public string? Nickname { get; set; }
        public bool IsFavorite { get; set; } = false;
    }
}
