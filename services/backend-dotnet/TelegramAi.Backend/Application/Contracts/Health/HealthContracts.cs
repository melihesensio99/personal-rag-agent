namespace TelegramAi.Backend.Application.Contracts.Health;
public sealed record AiServiceHealthResult(string Service, string Status, string Version);
