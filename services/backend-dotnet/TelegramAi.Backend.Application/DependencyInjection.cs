using Microsoft.Extensions.DependencyInjection;
using MediatR;
using TelegramAi.Backend.Application.Shared.Common.Behaviors;
using TelegramAi.Backend.Application.Features.Content.SemanticSearch;
using TelegramAi.Backend.Application.Features.Telegram.Formatting;
using TelegramAi.Backend.Application.Features.Telegram.Agents;

namespace TelegramAi.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IContentCreationWorkflow, ContentCreationWorkflow>();
        services.AddScoped<IRerankingService, RerankingService>();
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();
        services.AddScoped<ISemanticAnswerService, SemanticAnswerService>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IAgentToolExecutor, AgentToolExecutor>();
        services.AddSingleton<ITelegramMessageResponseFormatter, TelegramMessageResponseFormatter>();
        services.AddSingleton<ITelegramContentSearchResponseFormatter, TelegramContentSearchResponseFormatter>();
        services.AddSingleton<ITelegramSemanticAnswerResponseFormatter, TelegramSemanticAnswerResponseFormatter>();

        return services;
    }
}
