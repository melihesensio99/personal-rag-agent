namespace TelegramAi.Backend.Api.Contracts.Health;

public sealed record BackendHealthResponse(
    string Service,
    string Status,
    string Version);
