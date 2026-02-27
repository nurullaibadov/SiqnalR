using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Domain.Entities;
using UpAllNight.Domain.Interfaces;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class DevicesController : BaseController
    {
        private readonly IUnitOfWork _uow;

        public DevicesController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>Cihaz token kaydet</summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto request, CancellationToken ct)
        {
            var existing = await _uow.Repository<UserDevice>()
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken, ct);

            if (existing != null)
            {
                existing.UserId = CurrentUserId;
                existing.IsActive = true;
                existing.LastUsedAt = DateTime.UtcNow;
                existing.Platform = request.Platform;
                existing.DeviceName = request.DeviceName;
            }
            else
            {
                await _uow.Repository<UserDevice>().AddAsync(new UserDevice
                {
                    UserId = CurrentUserId,
                    DeviceToken = request.DeviceToken,
                    Platform = request.Platform,
                    DeviceName = request.DeviceName
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Ok(new { success = true, message = "Cihaz kaydedildi." });
        }

        /// <summary>Cihaz token sil (logout)</summary>
        [HttpDelete("{deviceToken}")]
        public async Task<IActionResult> UnregisterDevice(string deviceToken, CancellationToken ct)
        {
            var device = await _uow.Repository<UserDevice>()
                .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken && d.UserId == CurrentUserId, ct);

            if (device != null)
            {
                device.IsActive = false;
                await _uow.SaveChangesAsync(ct);
            }

            return Ok(new { success = true, message = "Cihaz kaydı silindi." });
        }
    }

    public record RegisterDeviceDto(
        [Required] string DeviceToken,
        [Required] DevicePlatform Platform,
        string? DeviceName
    );
}
