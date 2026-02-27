using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface IRealtimeService
    {
        Task SendToConversationAsync(string conversationId, string method, object data, CancellationToken ct = default);
        Task SendToUserAsync(string userId, string method, object data, CancellationToken ct = default);
        Task SendToGroupAsync(string groupName, string method, object data, CancellationToken ct = default);
    }
}
