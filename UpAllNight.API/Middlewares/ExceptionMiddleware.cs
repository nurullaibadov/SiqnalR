using System.Net;
using System.Text.Json;
using UpAllNight.Application.Common;

namespace UpAllNight.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                ArgumentException => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { exception.Message }
                },
                UnauthorizedAccessException => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Yetkisiz erişim." }
                },
                _ => new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." }
                }
            };

            context.Response.StatusCode = exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            response.TraceId = context.TraceIdentifier;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
