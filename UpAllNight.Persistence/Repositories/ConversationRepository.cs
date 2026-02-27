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
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(AppDbContext context) : base(context) { }

        public async Task<Conversation?> GetPrivateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(c => c.Type == ConversationType.Private)
                .Where(c => c.Participants.Any(p => p.UserId == user1Id && !p.HasLeft))
                .Where(c => c.Participants.Any(p => p.UserId == user2Id && !p.HasLeft))
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(c => c.Participants.Any(p => p.UserId == userId && !p.HasLeft))
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.LastMessage).ThenInclude(m => m!.Sender)
                .OrderByDescending(c => c.LastMessage!.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbSet
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            return conversation;
        }

        public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
            => await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId && !p.HasLeft, cancellationToken);

        public async Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
            => await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);
    }
}
