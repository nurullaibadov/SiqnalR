using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null, CancellationToken cancellationToken = default);
        Task SendBulkPushNotificationAsync(IEnumerable<Guid> userIds, string title, string body, object? data = null, CancellationToken cancellationToken = default);
        Task SendMessageNotificationAsync(Message message, CancellationToken cancellationToken = default);
    }
}
