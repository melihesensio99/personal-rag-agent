using TelegramAi.Backend.Api.Contracts.Intents;

namespace TelegramAi.Backend.Application.Telegram.Agents;

public interface IAgentToolExecutor
{
    Task<IReadOnlyList<string>> ExecuteAsync(
        AgentPlan plan,
        long chatId,
        string fallbackText,
        string? senderDisplayName,
        CancellationToken cancellationToken);
}
