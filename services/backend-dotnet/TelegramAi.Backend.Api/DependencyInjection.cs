using FluentValidation;

namespace TelegramAi.Backend.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddCors(options => options.AddPolicy("Frontend", policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(
                "http://localhost:3000", "http://127.0.0.1:3000",
                "http://localhost:5173", "http://127.0.0.1:5173")));
        return services;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapContentEndpoints();
        app.MapSearchEndpoints();
        app.MapSummaryEndpoints();

        return app;
    }
}
