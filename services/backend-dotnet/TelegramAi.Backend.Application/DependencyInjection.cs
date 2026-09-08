using Microsoft.Extensions.DependencyInjection;
using TelegramAi.Backend.Application.Features.Content.Services;
using TelegramAi.Backend.Application.Features.Telegram.Formatting;
using TelegramAi.Backend.Application.Features.Telegram.Agents;
using TelegramAi.Backend.Application.Features.Telegram.Services;
using TelegramAi.Backend.Application.Features.Content.Handlers;

namespace TelegramAi.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IContentApplicationService, ContentApplicationService>();
        services.AddScoped<IListContentsHandler, ListContentsHandler>();
        services.AddScoped<ICreateContentHandler, CreateContentHandler>();
        services.AddScoped<IGetContentHandler, GetContentHandler>();
        services.AddScoped<IGetContentChunksHandler, GetContentChunksHandler>();
        services.AddScoped<IListContentHandler, ListContentHandler>();
        services.AddScoped<ITelegramMessageApplicationService, TelegramMessageApplicationService>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IAgentToolExecutor, AgentToolExecutor>();
        services.AddSingleton<ITelegramMessageResponseFormatter, TelegramMessageResponseFormatter>();
        services.AddSingleton<ITelegramContentSearchResponseFormatter, TelegramContentSearchResponseFormatter>();
        services.AddSingleton<ITelegramSemanticAnswerResponseFormatter, TelegramSemanticAnswerResponseFormatter>();

        return services;
    }
}
