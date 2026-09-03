namespace TelegramAi.Backend.Application.Telegram.Agents;

public interface IAgentOrchestrator
{
    Task<IReadOnlyList<string>> ExecuteAsync(
        long chatId,
        string text,
        string? senderDisplayName,
        CancellationToken cancellationToken);
}
