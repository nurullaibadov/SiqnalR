using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Notifications.Services
{
    public interface INotificationAppService
    {
        Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
        Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
        Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
        Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
        Task<Result> DeleteNotificationAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
        Task<Result> CreateNotificationAsync(Guid userId, string title, string body, NotificationType type, string? data = null, CancellationToken ct = default);
    }

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? Data { get; set; }
        public string? ImageUrl { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationAppService : INotificationAppService
    {
        private readonly IUnitOfWork _uow;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public NotificationAppService(IUnitOfWork uow, IHubContext<NotificationHub> notificationHub)
        {
            _uow = uow;
            _notificationHub = notificationHub;
        }

        public async Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            var (items, total) = await _uow.Repository<Notification>().GetPagedAsync(
                predicate: n => n.UserId == userId,
                orderBy: q => q.OrderByDescending(n => n.CreatedAt),
                pageNumber: page,
                pageSize: pageSize,
                cancellationToken: ct
            );

            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                Data = n.Data,
                ImageUrl = n.ImageUrl,
                ActionUrl = n.ActionUrl,
                CreatedAt = n.CreatedAt
            });

            return Result<PagedResult<NotificationDto>>.Success(new PagedResult<NotificationDto>
            {
                Items = dtos,
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        {
            var count = await _uow.Repository<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
            return Result<int>.Success(count);
        }

        public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
        {
            var notification = await _uow.Repository<Notification>().GetByIdAsync(notificationId, ct);
            if (notification == null || notification.UserId != userId)
                return Result.Failure("Bildirim bulunamadı.", 404);

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }

        public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        {
            var unread = await _uow.Repository<Notification>()
                .FindAsync(n => n.UserId == userId && !n.IsRead, ct);

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Tüm bildirimler okundu olarak işaretlendi.");
        }

        public async Task<Result> DeleteNotificationAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
        {
            var notification = await _uow.Repository<Notification>().GetByIdAsync(notificationId, ct);
            if (notification == null || notification.UserId != userId)
                return Result.Failure("Bildirim bulunamadı.", 404);

            await _uow.Repository<Notification>().DeleteAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Bildirim silindi.");
        }

        public async Task<Result> CreateNotificationAsync(Guid userId, string title, string body, NotificationType type, string? data = null, CancellationToken ct = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                Data = data
            };

            await _uow.Repository<Notification>().AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);

            // Real-time bildirim
            await _notificationHub.Clients
                .Group($"user_{userId}")
                .SendAsync("NewNotification", new NotificationDto
                {
                    Id = notification.Id,
                    Title = title,
                    Body = body,
                    Type = type,
                    IsRead = false,
                    Data = data,
                    CreatedAt = notification.CreatedAt
                }, ct);

            return Result.Success();
        }
    }
}
