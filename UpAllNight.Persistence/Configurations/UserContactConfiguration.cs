using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Persistence.Configurations
{
    public class UserContactConfiguration : IEntityTypeConfiguration<UserContact>
    {
        public void Configure(EntityTypeBuilder<UserContact> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.OwnerId, x.ContactUserId }).IsUnique();

            builder.HasOne(x => x.Owner)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ContactUser)
                .WithMany(x => x.ContactOf)
                .HasForeignKey(x => x.ContactUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
