using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TelegramAi.Backend.Application.Shared.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        logger.LogDebug("Handling MediatR request {RequestName}", name);
        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogDebug("Handled MediatR request {RequestName} in {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(exception, "MediatR request {RequestName} failed after {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
