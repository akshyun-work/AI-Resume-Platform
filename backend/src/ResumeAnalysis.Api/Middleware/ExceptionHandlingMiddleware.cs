using System.Net;
using System.Text.Json;
using ResumeAnalysis.Api.DTOs.Common;

namespace ResumeAnalysis.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var traceId = context.TraceIdentifier;
        HttpStatusCode status;
        string message;

        switch (ex)
        {
            case InvalidOperationException ioe:
                status = HttpStatusCode.Conflict;
                message = ioe.Message;
                _logger.LogWarning(ex, "Conflict {TraceId}", traceId);
                break;
            case UnauthorizedAccessException:
                status = HttpStatusCode.Unauthorized;
                message = ex.Message;
                _logger.LogWarning(ex, "Unauthorized {TraceId}", traceId);
                break;
            case KeyNotFoundException knf:
                status = HttpStatusCode.NotFound;
                message = knf.Message;
                _logger.LogWarning(ex, "Not found {TraceId}", traceId);
                break;
            case FileNotFoundException fnf:
                status = HttpStatusCode.NotFound;
                message = fnf.Message;
                _logger.LogWarning(ex, "File not found {TraceId}", traceId);
                break;
            case ArgumentException:
                status = HttpStatusCode.BadRequest;
                message = ex.Message;
                _logger.LogWarning(ex, "Bad request {TraceId}", traceId);
                break;
            default:
                status = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred.";
                _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = message,
            TraceId = traceId
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
