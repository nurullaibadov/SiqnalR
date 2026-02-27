using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Infrastructure.Services
{
    public interface INotificationHubService
    {
        Task SendNotificationAsync(string userId, object notification, CancellationToken ct = default);
    }
}
