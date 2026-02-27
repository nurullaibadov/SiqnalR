using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpAllNight.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? Message { get; private set; }
        public List<string> Errors { get; private set; } = new();
        public int StatusCode { get; private set; }

        private Result() { }

        public static Result<T> Success(T data, string? message = null, int statusCode = 200)
            => new() { IsSuccess = true, Data = data, Message = message, StatusCode = statusCode };

        public static Result<T> Failure(string error, int statusCode = 400)
            => new() { IsSuccess = false, Errors = new List<string> { error }, StatusCode = statusCode };

        public static Result<T> Failure(List<string> errors, int statusCode = 400)
            => new() { IsSuccess = false, Errors = errors, StatusCode = statusCode };

        public static Result<T> NotFound(string message = "Resource not found")
            => new() { IsSuccess = false, Errors = new List<string> { message }, StatusCode = 404 };

        public static Result<T> Unauthorized(string message = "Unauthorized")
            => new() { IsSuccess = false, Errors = new List<string> { message }, StatusCode = 401 };

        public static Result<T> Forbidden(string message = "Forbidden")
            => new() { IsSuccess = false, Errors = new List<string> { message }, StatusCode = 403 };
    }

    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? Message { get; private set; }
        public List<string> Errors { get; private set; } = new();
        public int StatusCode { get; private set; }

        private Result() { }

        public static Result Success(string? message = null, int statusCode = 200)
            => new() { IsSuccess = true, Message = message, StatusCode = statusCode };

        public static Result Failure(string error, int statusCode = 400)
            => new() { IsSuccess = false, Errors = new List<string> { error }, StatusCode = statusCode };

        public static Result Failure(List<string> errors, int statusCode = 400)
            => new() { IsSuccess = false, Errors = errors, StatusCode = statusCode };
    }
}
