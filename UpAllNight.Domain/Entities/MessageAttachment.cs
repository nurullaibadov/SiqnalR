using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Common;

namespace UpAllNight.Domain.Entities
{
    public class MessageAttachment : BaseEntity
    {
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public AttachmentType Type { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }
    }

    public enum AttachmentType
    {
        Image = 0,
        Video = 1,
        Audio = 2,
        Document = 3,
        Other = 4
    }
}
