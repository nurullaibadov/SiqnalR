using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface IChatNotificationService
    {
        Task SendMessageAsync(string conversationId, object message, CancellationToken ct = default);
        Task MessageEditedAsync(string conversationId, object message, CancellationToken ct = default);
        Task MessageDeletedAsync(string conversationId, object data, CancellationToken ct = default);
        Task ReactionAsync(string conversationId, object data, CancellationToken ct = default);
        Task MessagesReadAsync(string conversationId, object data, CancellationToken ct = default);
    }

    public interface INotificationSenderService
    {
        Task SendToUserAsync(string userId, object notification, CancellationToken ct = default);
    }
}
