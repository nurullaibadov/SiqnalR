using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Dictionary<string, string> _userConnections = new();

        public ChatHub(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                _userConnections[userId] = Context.ConnectionId;

                // Kullanıcıyı online olarak işaretle
                var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(userId));
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeenAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                }

                // Kullanıcının konuşmalarına join et
                var conversations = await _unitOfWork.Conversations.GetUserConversationsAsync(Guid.Parse(userId));
                foreach (var conv in conversations)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conv.Id}");

                // Diğer kullanıcılara online olduğunu bildir
                await Clients.Others.SendAsync("UserOnline", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                _userConnections.Remove(userId);

                var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(userId));
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeenAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                }

                await Clients.Others.SendAsync("UserOffline", userId, DateTime.UtcNow);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task SendTyping(string conversationId, bool isTyping)
        {
            var userId = Context.UserIdentifier;
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserTyping", new { UserId = userId, ConversationId = conversationId, IsTyping = isTyping });
        }

        public async Task MarkMessagesAsRead(string conversationId)
        {
            var userId = Guid.Parse(Context.UserIdentifier!);
            await _unitOfWork.Messages.MarkAsReadAsync(Guid.Parse(conversationId), userId);
            await _unitOfWork.SaveChangesAsync();

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("MessagesRead", new { UserId = userId, ConversationId = conversationId });
        }

        public static string? GetConnectionId(string userId)
        {
            _userConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}
