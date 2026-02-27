using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Users.DTOs;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Admin.Services
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int BannedUsers { get; set; }
        public int TotalConversations { get; set; }
        public int TotalGroups { get; set; }
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int TodayNewUsers { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IAdminService
    {
        Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(CancellationToken ct = default);
        Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(PaginationRequest request, CancellationToken ct = default);
        Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
        Task<Result> UpdateUserRoleAsync(Guid adminId, Guid userId, UserRole role, CancellationToken ct = default);
        Task<Result> BanUserAsync(Guid adminId, Guid userId, string? reason, DateTime? bannedUntil, CancellationToken ct = default);
        Task<Result> UnbanUserAsync(Guid adminId, Guid userId, CancellationToken ct = default);
        Task<Result> DeactivateUserAsync(Guid adminId, Guid userId, CancellationToken ct = default);
        Task<Result> ActivateUserAsync(Guid adminId, Guid userId, CancellationToken ct = default);
        Task<Result> DeleteUserAsync(Guid adminId, Guid userId, CancellationToken ct = default);
        Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct = default);
        Task<Result> DeleteConversationAsync(Guid adminId, Guid conversationId, CancellationToken ct = default);
        Task<Result> DeleteMessageAsync(Guid adminId, Guid messageId, CancellationToken ct = default);
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AdminService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;

            var stats = new DashboardStatsDto
            {
                TotalUsers = await _uow.Users.CountAsync(null, ct),
                ActiveUsers = await _uow.Users.CountAsync(u => u.IsActive, ct),
                OnlineUsers = await _uow.Users.CountAsync(u => u.IsOnline, ct),
                BannedUsers = await _uow.Users.CountAsync(u => u.IsBanned, ct),
                TotalConversations = await _uow.Conversations.CountAsync(null, ct),
                TotalGroups = await _uow.Conversations.CountAsync(c => c.Type == ConversationType.Group, ct),
                TotalMessages = await _uow.Messages.CountAsync(null, ct),
                TodayMessages = await _uow.Messages.CountAsync(m => m.CreatedAt >= today, ct),
                TodayNewUsers = await _uow.Users.CountAsync(u => u.CreatedAt >= today, ct)
            };

            return Result<DashboardStatsDto>.Success(stats);
        }

        public async Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, total) = await _uow.Users.GetPagedAsync(
                predicate: string.IsNullOrWhiteSpace(request.SearchTerm) ? null :
                    u => u.UserName.Contains(request.SearchTerm) ||
                         u.Email.Contains(request.SearchTerm) ||
                         u.PhoneNumber.Contains(request.SearchTerm),
                orderBy: request.SortBy switch
                {
                    "email" => q => request.SortDescending ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email),
                    "username" => q => request.SortDescending ? q.OrderByDescending(u => u.UserName) : q.OrderBy(u => u.UserName),
                    _ => q => request.SortDescending ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt)
                },
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: ct
            );

            return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = _mapper.Map<IEnumerable<UserDto>>(items),
                TotalCount = total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result<UserDto>.NotFound("Kullanıcı bulunamadı.");
            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result> UpdateUserRoleAsync(Guid adminId, Guid userId, UserRole role, CancellationToken ct = default)
        {
            var admin = await _uow.Users.GetByIdAsync(adminId, ct);
            if (admin == null || admin.Role < UserRole.Admin) return Result.Forbidden("Yetersiz yetki.");

            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            if (role >= admin.Role) return Result.Failure("Kendi rolünüzden yüksek bir rol atayamazsınız.", 403);

            user.Role = role;
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "UpdateRole", "User", userId.ToString(), ct);
            return Result.Success($"Kullanıcı rolü {role} olarak güncellendi.");
        }

        public async Task<Result> BanUserAsync(Guid adminId, Guid userId, string? reason, DateTime? bannedUntil, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);
            if (user.Role >= UserRole.Admin) return Result.Failure("Admin kullanıcıları yasaklanamaz.", 403);

            user.IsBanned = true;
            user.BanReason = reason;
            user.BannedUntil = bannedUntil;
            user.RefreshToken = null; // Oturumu sonlandır

            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "BanUser", "User", userId.ToString(), ct);
            return Result.Success("Kullanıcı yasaklandı.");
        }

        public async Task<Result> UnbanUserAsync(Guid adminId, Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            user.IsBanned = false;
            user.BanReason = null;
            user.BannedUntil = null;

            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "UnbanUser", "User", userId.ToString(), ct);
            return Result.Success("Kullanıcı yasağı kaldırıldı.");
        }

        public async Task<Result> DeactivateUserAsync(Guid adminId, Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            user.IsActive = false;
            user.RefreshToken = null;
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "DeactivateUser", "User", userId.ToString(), ct);
            return Result.Success("Kullanıcı deaktif edildi.");
        }

        public async Task<Result> ActivateUserAsync(Guid adminId, Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            user.IsActive = true;
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "ActivateUser", "User", userId.ToString(), ct);
            return Result.Success("Kullanıcı aktif edildi.");
        }

        public async Task<Result> DeleteUserAsync(Guid adminId, Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);
            if (user.Role >= UserRole.Admin) return Result.Failure("Admin kullanıcıları silinemez.", 403);

            await _uow.Users.SoftDeleteAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "DeleteUser", "User", userId.ToString(), ct);
            return Result.Success("Kullanıcı silindi.");
        }

        public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, total) = await _uow.Repository<AuditLog>().GetPagedAsync(
                predicate: string.IsNullOrWhiteSpace(request.SearchTerm) ? null :
                    a => a.Action.Contains(request.SearchTerm) || a.EntityName.Contains(request.SearchTerm),
                orderBy: q => q.OrderByDescending(a => a.CreatedAt),
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: ct
            );

            var dtos = items.Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                IsSuccess = a.IsSuccess,
                ErrorMessage = a.ErrorMessage,
                CreatedAt = a.CreatedAt
            });

            return Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
            {
                Items = dtos,
                TotalCount = total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        public async Task<Result> DeleteConversationAsync(Guid adminId, Guid conversationId, CancellationToken ct = default)
        {
            var conv = await _uow.Conversations.GetByIdAsync(conversationId, ct);
            if (conv == null) return Result.Failure("Konuşma bulunamadı.", 404);

            await _uow.Conversations.SoftDeleteAsync(conv, ct);
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "DeleteConversation", "Conversation", conversationId.ToString(), ct);
            return Result.Success("Konuşma silindi.");
        }

        public async Task<Result> DeleteMessageAsync(Guid adminId, Guid messageId, CancellationToken ct = default)
        {
            var message = await _uow.Messages.GetByIdAsync(messageId, ct);
            if (message == null) return Result.Failure("Mesaj bulunamadı.", 404);

            await _uow.Messages.SoftDeleteAsync(message, ct);
            await _uow.SaveChangesAsync(ct);
            await LogAuditAsync(adminId, "DeleteMessage", "Message", messageId.ToString(), ct);
            return Result.Success("Mesaj silindi.");
        }

        private async Task LogAuditAsync(Guid userId, string action, string entityName, string entityId, CancellationToken ct)
        {
            await _uow.Repository<AuditLog>().AddAsync(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                IsSuccess = true
            }, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
