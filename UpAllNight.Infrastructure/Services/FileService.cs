using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly string _uploadPath;
        private readonly string _baseUrl;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedExtensions;

        public FileService(IConfiguration configuration)
        {
            _configuration = configuration;
            var fileSettings = configuration.GetSection("FileSettings");
            _uploadPath = fileSettings["UploadPath"] ?? "wwwroot/uploads";
            _baseUrl = fileSettings["BaseUrl"] ?? "https://localhost:7001";
            _maxFileSizeBytes = (long)(double.Parse(fileSettings["MaxFileSizeMB"] ?? "50") * 1024 * 1024);
            _allowedExtensions = fileSettings.GetSection("AllowedExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".pdf" };
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(_uploadPath, folder);

            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            return $"{_baseUrl}/{folder}/{fileName}";
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var newFileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(_uploadPath, folder);

            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, newFileName);
            using var file = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(file, cancellationToken);

            return $"{_baseUrl}/{folder}/{newFileName}";
        }

        public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            var relativePath = fileUrl.Replace(_baseUrl + "/", "");
            var filePath = Path.Combine(_uploadPath, relativePath);

            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            var relativePath = fileUrl.Replace(_baseUrl + "/", "");
            var filePath = Path.Combine(_uploadPath, relativePath);
            return Task.FromResult(File.Exists(filePath));
        }

        public string GetFileUrl(string filePath) => $"{_baseUrl}/{filePath}";

        public bool IsValidFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(long fileSize) => fileSize <= _maxFileSizeBytes;
    }
}
