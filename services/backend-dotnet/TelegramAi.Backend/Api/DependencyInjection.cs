namespace TelegramAi.Backend.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapContentEndpoints();
        app.MapSummaryEndpoints();

        return app;
    }
}
