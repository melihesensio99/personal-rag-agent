namespace TelegramAi.Backend.Api.Contracts.Health;

public sealed record BackendSystemHealthResponse(
    string Service,
    string Status,
    BackendDependencyHealthResponse Dependencies);
