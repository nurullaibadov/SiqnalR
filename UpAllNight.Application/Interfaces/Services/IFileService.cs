using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
        string GetFileUrl(string filePath);
        bool IsValidFileExtension(string fileName);
        bool IsValidFileSize(long fileSize);
    }
}
