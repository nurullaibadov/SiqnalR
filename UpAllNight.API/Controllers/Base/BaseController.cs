using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpAllNight.Application.Common;

namespace UpAllNight.API.Controllers.Base
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController : ControllerBase
    {
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.StatusCode switch
                {
                    201 => StatusCode(201, ApiResponse<T>.Ok(result.Data!, result.Message)),
                    204 => NoContent(),
                    _ => Ok(ApiResponse<T>.Ok(result.Data!, result.Message))
                };
            }

            return result.StatusCode switch
            {
                401 => Unauthorized(ApiResponse<T>.Fail(result.Errors)),
                403 => StatusCode(403, ApiResponse<T>.Fail(result.Errors)),
                404 => NotFound(ApiResponse<T>.Fail(result.Errors)),
                _ => BadRequest(ApiResponse<T>.Fail(result.Errors))
            };
        }

        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(new { success = true, message = result.Message });

            return result.StatusCode switch
            {
                401 => Unauthorized(new { success = false, errors = result.Errors }),
                403 => StatusCode(403, new { success = false, errors = result.Errors }),
                404 => NotFound(new { success = false, errors = result.Errors }),
                _ => BadRequest(new { success = false, errors = result.Errors })
            };
        }

        protected Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
            }
        }
    }
}
