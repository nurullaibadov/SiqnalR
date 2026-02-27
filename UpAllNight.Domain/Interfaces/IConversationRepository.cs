using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Domain.Interfaces
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<Conversation?> GetPrivateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
        Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
        Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    }
}
