using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Infrastructure.AiService;
using TelegramAi.Backend.Infrastructure.Persistence;
using TelegramAi.Backend.Infrastructure.Telegram;
using TelegramAi.Backend.Infrastructure.Telegram.TelegramApi;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace TelegramAi.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApplicationDb")
            ?? throw new InvalidOperationException("Connection string 'ApplicationDb' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.UseVector());
        });

        services.AddScoped<IContentRepository, EfCoreContentRepository>();
        services.AddHostedService<DatabaseMigrationHostedService>();
        services.AddHostedService<TelegramBotStartupDiagnosticsHostedService>();
        services.AddHostedService<TelegramPollingHostedService>();

        services
            .AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<TelegramBotOptions>()
            .Bind(configuration.GetSection(TelegramBotOptions.SectionName));

        services.AddHttpClient<ITelegramBotApiClient, TelegramBotApiClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://api.telegram.org/");
            httpClient.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IAiServiceClient, AiServiceClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            httpClient.BaseAddress = options.BaseUrl;
            httpClient.Timeout = options.Timeout;
        });

        return services;
    }
}
