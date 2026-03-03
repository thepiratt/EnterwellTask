using System.Net;
using System.Text.Json;

namespace QuizApp.Infrastructure.Middleware;

// Global exception handling middleware
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = exception.Message,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid argument: {exception.Message}";
                break;
            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid operation: {exception.Message}";
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred. Please try again later.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    private class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
