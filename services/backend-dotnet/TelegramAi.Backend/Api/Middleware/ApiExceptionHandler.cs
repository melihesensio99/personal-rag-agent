using System.Net.Mime;

namespace TelegramAi.Backend.Api.Middleware;

public sealed class ApiExceptionHandler(RequestDelegate next, ILogger<ApiExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/500",
                title = "Beklenmeyen bir hata oluştu.",
                status = 500,
                traceId = context.TraceIdentifier
            });
        }
    }
}
