using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Domain.Interfaces
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsDeliveredAsync(IEnumerable<Guid> messageIds, Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    }
}
