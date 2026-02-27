using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.Infrastructure.Services
{
    public class RealtimeService : IRealtimeService
    {
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public RealtimeService(IHubContext<ChatHub> chatHub, IHubContext<NotificationHub> notificationHub)
        {
            _chatHub = chatHub;
            _notificationHub = notificationHub;
        }

        public async Task SendToConversationAsync(string conversationId, string method, object data, CancellationToken ct = default)
            => await _chatHub.Clients.Group($"conversation_{conversationId}").SendAsync(method, data, ct);

        public async Task SendToUserAsync(string userId, string method, object data, CancellationToken ct = default)
            => await _notificationHub.Clients.Group($"user_{userId}").SendAsync(method, data, ct);

        public async Task SendToGroupAsync(string groupName, string method, object data, CancellationToken ct = default)
            => await _chatHub.Clients.Group(groupName).SendAsync(method, data, ct);
    }
}
