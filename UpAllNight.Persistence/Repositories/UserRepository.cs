using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;
using UpAllNight.Persistence.Context;

namespace UpAllNight.Persistence.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);

        public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.UserName.ToLower() == userName.ToLower(), cancellationToken);

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, cancellationToken);

        public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.EmailVerificationToken == token, cancellationToken);

        public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(x => x.PasswordResetToken == token, cancellationToken);

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
            => await _dbSet.Where(x =>
                x.UserName.Contains(searchTerm) ||
                x.Email.Contains(searchTerm) ||
                (x.FirstName != null && x.FirstName.Contains(searchTerm)) ||
                (x.LastName != null && x.LastName.Contains(searchTerm)) ||
                x.PhoneNumber.Contains(searchTerm))
                .Take(20)
                .ToListAsync(cancellationToken);

        public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);

        public async Task<bool> IsUserNameExistsAsync(string userName, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(x => x.UserName.ToLower() == userName.ToLower(), cancellationToken);

        public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }
}
