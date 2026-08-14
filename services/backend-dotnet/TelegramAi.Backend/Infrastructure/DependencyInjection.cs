using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Infrastructure.AiService;
using TelegramAi.Backend.Infrastructure.Persistence;
using TelegramAi.Backend.Infrastructure.Telegram;

namespace TelegramAi.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IContentRepository, InMemoryContentRepository>();
        services.AddHostedService<TelegramBotStartupDiagnosticsHostedService>();

        services
            .AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<TelegramBotOptions>()
            .Bind(configuration.GetSection(TelegramBotOptions.SectionName));

        services.AddHttpClient<IAiServiceClient, AiServiceClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            httpClient.BaseAddress = options.BaseUrl;
            httpClient.Timeout = options.Timeout;
        });

        return services;
    }
}
