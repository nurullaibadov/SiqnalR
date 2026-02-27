using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Common;
using UpAllNight.Application.Features.Users.DTOs;
using UpAllNight.Application.Interfaces.Services;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.Application.Features.Users.Services
{
    public interface IUserService
    {
        Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
        Task<Result<UserProfileDto>> GetProfileByUserNameAsync(string userName, CancellationToken ct = default);
        Task<Result<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken ct = default);
        Task<Result<string>> UploadProfilePictureAsync(Guid userId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default);
        Task<Result> DeleteProfilePictureAsync(Guid userId, CancellationToken ct = default);
        Task<Result<Common.PagedResult<UserDto>>> SearchUsersAsync(string searchTerm, int page, int pageSize, CancellationToken ct = default);
        Task<Result<IEnumerable<UserDto>>> GetContactsAsync(Guid userId, CancellationToken ct = default);
        Task<Result> AddContactAsync(Guid userId, Guid contactId, string? nickname, CancellationToken ct = default);
        Task<Result> RemoveContactAsync(Guid userId, Guid contactId, CancellationToken ct = default);
        Task<Result> BlockUserAsync(Guid userId, Guid targetId, string? reason, CancellationToken ct = default);
        Task<Result> UnblockUserAsync(Guid userId, Guid targetId, CancellationToken ct = default);
        Task<Result<IEnumerable<UserDto>>> GetBlockedUsersAsync(Guid userId, CancellationToken ct = default);
        Task<Result> UpdateOnlineStatusAsync(Guid userId, bool isOnline, CancellationToken ct = default);
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public UserService(IUnitOfWork uow, IMapper mapper, IFileService fileService)
        {
            _uow = uow;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result<UserProfileDto>.NotFound("Kullanıcı bulunamadı.");

            var dto = _mapper.Map<UserProfileDto>(user);
            dto.ContactsCount = (await _uow.Repository<UserContact>()
                .FindAsync(c => c.OwnerId == userId, ct)).Count();
            return Result<UserProfileDto>.Success(dto);
        }

        public async Task<Result<UserProfileDto>> GetProfileByUserNameAsync(string userName, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByUserNameAsync(userName, ct);
            if (user == null) return Result<UserProfileDto>.NotFound("Kullanıcı bulunamadı.");
            return Result<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user));
        }

        public async Task<Result<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result<UserProfileDto>.NotFound();

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                if (await _uow.Users.IsPhoneNumberExistsAsync(request.PhoneNumber, ct))
                    return Result<UserProfileDto>.Failure("Bu telefon numarası başka bir hesapta kullanılıyor.");
                user.PhoneNumber = request.PhoneNumber;
            }

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.Bio = request.Bio ?? user.Bio;
            user.Status = request.Status ?? user.Status;

            await _uow.SaveChangesAsync(ct);
            return Result<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user), "Profil güncellendi.");
        }

        public async Task<Result<string>> UploadProfilePictureAsync(Guid userId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default)
        {
            if (!_fileService.IsValidFileExtension(file.FileName))
                return Result<string>.Failure("Geçersiz dosya türü. Sadece JPG, JPEG, PNG, GIF kabul edilir.");

            if (!_fileService.IsValidFileSize(file.Length))
                return Result<string>.Failure("Dosya boyutu çok büyük. Maksimum 5MB.");

            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result<string>.NotFound();

            // Eski fotoğrafı sil
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                await _fileService.DeleteFileAsync(user.ProfilePictureUrl, ct);

            var url = await _fileService.UploadFileAsync(file, "profiles", ct);
            user.ProfilePictureUrl = url;
            await _uow.SaveChangesAsync(ct);

            return Result<string>.Success(url, "Profil fotoğrafı güncellendi.");
        }

        public async Task<Result> DeleteProfilePictureAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                await _fileService.DeleteFileAsync(user.ProfilePictureUrl, ct);

            user.ProfilePictureUrl = null;
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Profil fotoğrafı silindi.");
        }

        public async Task<Result<PagedResult<UserDto>>> SearchUsersAsync(string searchTerm, int page, int pageSize, CancellationToken ct = default)
        {
            var users = await _uow.Users.SearchUsersAsync(searchTerm, ct);
            var dtos = _mapper.Map<IEnumerable<UserDto>>(users).ToList();

            return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = dtos.Skip((page - 1) * pageSize).Take(pageSize),
                TotalCount = dtos.Count,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        public async Task<Result<IEnumerable<UserDto>>> GetContactsAsync(Guid userId, CancellationToken ct = default)
        {
            var contacts = await _uow.Repository<UserContact>()
                .FindAsync(c => c.OwnerId == userId, ct);

            var users = new List<User>();
            foreach (var contact in contacts)
            {
                var user = await _uow.Users.GetByIdAsync(contact.ContactUserId, ct);
                if (user != null) users.Add(user);
            }

            return Result<IEnumerable<UserDto>>.Success(_mapper.Map<IEnumerable<UserDto>>(users));
        }

        public async Task<Result> AddContactAsync(Guid userId, Guid contactId, string? nickname, CancellationToken ct = default)
        {
            if (userId == contactId) return Result.Failure("Kendinizi ekleyemezsiniz.");

            var contactUser = await _uow.Users.GetByIdAsync(contactId, ct);
            if (contactUser == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            var exists = await _uow.Repository<UserContact>()
                .AnyAsync(c => c.OwnerId == userId && c.ContactUserId == contactId, ct);

            if (exists) return Result.Failure("Bu kullanıcı zaten rehberinizdde.");

            await _uow.Repository<UserContact>().AddAsync(new UserContact
            {
                OwnerId = userId,
                ContactUserId = contactId,
                Nickname = nickname
            }, ct);

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Kişi eklendi.");
        }

        public async Task<Result> RemoveContactAsync(Guid userId, Guid contactId, CancellationToken ct = default)
        {
            var contact = await _uow.Repository<UserContact>()
                .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ContactUserId == contactId, ct);

            if (contact == null) return Result.Failure("Kişi bulunamadı.", 404);

            await _uow.Repository<UserContact>().DeleteAsync(contact, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Kişi silindi.");
        }

        public async Task<Result> BlockUserAsync(Guid userId, Guid targetId, string? reason, CancellationToken ct = default)
        {
            if (userId == targetId) return Result.Failure("Kendinizi engelleyemezsiniz.");

            var exists = await _uow.Repository<UserBlock>()
                .AnyAsync(b => b.BlockerId == userId && b.BlockedUserId == targetId, ct);

            if (exists) return Result.Failure("Bu kullanıcı zaten engellenmiş.");

            await _uow.Repository<UserBlock>().AddAsync(new UserBlock
            {
                BlockerId = userId,
                BlockedUserId = targetId,
                Reason = reason
            }, ct);

            await _uow.SaveChangesAsync(ct);
            return Result.Success("Kullanıcı engellendi.");
        }

        public async Task<Result> UnblockUserAsync(Guid userId, Guid targetId, CancellationToken ct = default)
        {
            var block = await _uow.Repository<UserBlock>()
                .FirstOrDefaultAsync(b => b.BlockerId == userId && b.BlockedUserId == targetId, ct);

            if (block == null) return Result.Failure("Engellenmiş kullanıcı bulunamadı.", 404);

            await _uow.Repository<UserBlock>().DeleteAsync(block, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success("Engel kaldırıldı.");
        }

        public async Task<Result<IEnumerable<UserDto>>> GetBlockedUsersAsync(Guid userId, CancellationToken ct = default)
        {
            var blocks = await _uow.Repository<UserBlock>()
                .FindAsync(b => b.BlockerId == userId, ct);

            var users = new List<User>();
            foreach (var block in blocks)
            {
                var user = await _uow.Users.GetByIdAsync(block.BlockedUserId, ct);
                if (user != null) users.Add(user);
            }

            return Result<IEnumerable<UserDto>>.Success(_mapper.Map<IEnumerable<UserDto>>(users));
        }

        public async Task<Result> UpdateOnlineStatusAsync(Guid userId, bool isOnline, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null) return Result.Failure("Kullanıcı bulunamadı.", 404);

            user.IsOnline = isOnline;
            user.LastSeenAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
