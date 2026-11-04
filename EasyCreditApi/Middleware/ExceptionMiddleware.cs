using EasyCreditApi.Application.DTO;

namespace EasyCreditApi.Middleware;

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
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (BadHttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ConflictException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Internal server error"));
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
