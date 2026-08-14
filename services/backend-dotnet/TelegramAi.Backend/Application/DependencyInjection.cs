using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Services;

namespace TelegramAi.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IContentApplicationService, ContentApplicationService>();
        services.AddScoped<ITelegramMessageApplicationService, TelegramMessageApplicationService>();

        return services;
    }
}
