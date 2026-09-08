
namespace TelegramAi.Backend.Application.Features.Telegram.Agents;

public interface IAgentToolExecutor
{
    Task<IReadOnlyList<string>> ExecuteAsync(
        AgentPlan plan,
        long chatId,
        string fallbackText,
        string? senderDisplayName,
        CancellationToken cancellationToken);
}
