using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Application.Telegram.Agents;
using TelegramAi.Backend.Application.Telegram.Services;

namespace TelegramAi.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IContentApplicationService, ContentApplicationService>();
        services.AddScoped<ITelegramMessageApplicationService, TelegramMessageApplicationService>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IAgentToolExecutor, AgentToolExecutor>();
        services.AddSingleton<ITelegramMessageResponseFormatter, TelegramMessageResponseFormatter>();
        services.AddSingleton<ITelegramContentSearchResponseFormatter, TelegramContentSearchResponseFormatter>();
        services.AddSingleton<ITelegramSemanticAnswerResponseFormatter, TelegramSemanticAnswerResponseFormatter>();

        return services;
    }
}
