namespace TelegramAi.Backend.Api.Contracts.Health;

public sealed record AiServiceHealthResponse(
    string Service,
    string Status,
    string Version);
