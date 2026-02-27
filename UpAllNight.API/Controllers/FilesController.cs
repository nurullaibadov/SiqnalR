using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.API.Controllers.Base;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    public class FilesController : BaseController
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>Tek dosya yükle</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(52_428_800)]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string folder = "general", CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Dosya seçilmedi." });

            if (!_fileService.IsValidFileExtension(file.FileName))
                return BadRequest(new { success = false, message = "Geçersiz dosya türü." });

            if (!_fileService.IsValidFileSize(file.Length))
                return BadRequest(new { success = false, message = "Dosya çok büyük. Maksimum 50MB." });

            var url = await _fileService.UploadFileAsync(file, folder, ct);
            return Ok(new { success = true, url, fileName = file.FileName, size = file.Length });
        }

        /// <summary>Çoklu dosya yükle</summary>
        [HttpPost("upload-multiple")]
        [RequestSizeLimit(104_857_600)] // 100MB
        public async Task<IActionResult> UploadMultipleFiles(List<IFormFile> files, [FromQuery] string folder = "general", CancellationToken ct = default)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { success = false, message = "Dosya seçilmedi." });

            if (files.Count > 10)
                return BadRequest(new { success = false, message = "Maksimum 10 dosya yükleyebilirsiniz." });

            var results = new List<object>();
            foreach (var file in files)
            {
                if (!_fileService.IsValidFileExtension(file.FileName) || !_fileService.IsValidFileSize(file.Length))
                {
                    results.Add(new { success = false, fileName = file.FileName, error = "Geçersiz dosya." });
                    continue;
                }

                var url = await _fileService.UploadFileAsync(file, folder, ct);
                results.Add(new { success = true, url, fileName = file.FileName, size = file.Length });
            }

            return Ok(new { success = true, files = results });
        }

        /// <summary>Dosya sil</summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return BadRequest(new { success = false, message = "Dosya URL gereklidir." });

            await _fileService.DeleteFileAsync(fileUrl, ct);
            return Ok(new { success = true, message = "Dosya silindi." });
        }
    }
}
