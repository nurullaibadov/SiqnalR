using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
        Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
        Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> IsUserNameExistsAsync(string userName, CancellationToken cancellationToken = default);
        Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);
    }
}
