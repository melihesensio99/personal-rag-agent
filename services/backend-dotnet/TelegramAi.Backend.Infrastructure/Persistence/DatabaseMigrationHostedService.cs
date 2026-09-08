using Microsoft.EntityFrameworkCore;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class DatabaseMigrationHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying Entity Framework Core migrations.");
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
