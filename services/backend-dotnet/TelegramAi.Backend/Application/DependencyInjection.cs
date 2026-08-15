using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Classification;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Application.Telegram.Interpretation;
using TelegramAi.Backend.Application.Telegram.Services;

namespace TelegramAi.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IContentApplicationService, ContentApplicationService>();
        services.AddScoped<ITelegramMessageApplicationService, TelegramMessageApplicationService>();
        services.AddSingleton<ITelegramContentSourceDetector, TelegramContentSourceDetector>();
        services.AddSingleton<ITelegramMessageResponseFormatter, TelegramMessageResponseFormatter>();
        services.AddSingleton<ITelegramContentSearchResponseFormatter, TelegramContentSearchResponseFormatter>();
        services.AddSingleton<ITelegramMessageIntentInterpreter, RuleBasedTelegramMessageIntentInterpreter>();

        return services;
    }
}
