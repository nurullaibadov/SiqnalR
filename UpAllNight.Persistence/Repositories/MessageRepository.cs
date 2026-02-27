using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;
using UpAllNight.Persistence.Context;

namespace UpAllNight.Persistence.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Include(m => m.Reactions).ThenInclude(r => r.User)
                .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId)
                .Where(m => !m.StatusTrackers.Any(st => st.UserId == userId && st.Status == MessageStatus.Read))
                .ToListAsync(cancellationToken);

        public async Task MarkAsDeliveredAsync(IEnumerable<Guid> messageIds, Guid userId, CancellationToken cancellationToken = default)
        {
            var trackers = messageIds.Select(mid => new MessageStatus_Tracker
            {
                MessageId = mid,
                UserId = userId,
                Status = MessageStatus.Delivered,
                DeliveredAt = DateTime.UtcNow
            });
            await _context.MessageStatusTrackers.AddRangeAsync(trackers, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
        {
            var unreadMessages = await GetUnreadMessagesAsync(conversationId, userId, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var message in unreadMessages)
            {
                var existing = await _context.MessageStatusTrackers
                    .FirstOrDefaultAsync(st => st.MessageId == message.Id && st.UserId == userId, cancellationToken);

                if (existing == null)
                {
                    await _context.MessageStatusTrackers.AddAsync(new MessageStatus_Tracker
                    {
                        MessageId = message.Id,
                        UserId = userId,
                        Status = MessageStatus.Read,
                        DeliveredAt = now,
                        ReadAt = now
                    }, cancellationToken);
                }
                else
                {
                    existing.Status = MessageStatus.Read;
                    existing.ReadAt = now;
                }
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
            => await _dbSet
                .CountAsync(m =>
                    m.ConversationId == conversationId &&
                    m.SenderId != userId &&
                    !m.StatusTrackers.Any(st => st.UserId == userId && st.Status == MessageStatus.Read),
                    cancellationToken);
    }
}
