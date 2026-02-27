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
    public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(EntityTypeBuilder<UserBlock> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.BlockerId, x.BlockedUserId }).IsUnique();

            builder.HasOne(x => x.Blocker)
                .WithMany(x => x.BlockedUsers)
                .HasForeignKey(x => x.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.BlockedUser)
                .WithMany(x => x.BlockedBy)
                .HasForeignKey(x => x.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
