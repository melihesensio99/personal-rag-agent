using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Application.Telegram.Exceptions;
using TelegramAi.Backend.Infrastructure.AiService;

namespace TelegramAi.Backend.Application.Telegram.Agents;

public sealed class AgentOrchestrator(
    IAiServiceClient aiServiceClient,
    IAgentToolExecutor toolExecutor) : IAgentOrchestrator
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(
        long chatId,
        string text,
        string? senderDisplayName,
        CancellationToken cancellationToken)
    {
        var decision = await ExecuteIntentClassificationAsync(text, cancellationToken);
        var plan = BuildAgentPlan(text, decision);
        return await toolExecutor.ExecuteAsync(plan, chatId, text, senderDisplayName, cancellationToken);
    }

    private async Task<ClassifyIntentResponse> ExecuteIntentClassificationAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            return await aiServiceClient.ClassifyIntentAsync(
                new ClassifyIntentRequest(text, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AiIntentUnavailableException(exception);
        }
    }

    private static AgentPlan BuildAgentPlan(string text, ClassifyIntentResponse decision)
    {
        var tool = decision.Action.ToLowerInvariant() switch
        {
            "list_contents" => AgentTool.SearchSavedContent,
            "answer_from_memory" => AgentTool.AnswerUsingSavedContent,
            "ask_clarification" => AgentTool.AskUserForClarification,
            _ => AgentTool.SaveIncomingContent,
        };

        return new AgentPlan(text, tool, decision, [new AgentToolCall(tool, new Dictionary<string, object?>
        {
            ["query"] = decision.Query,
            ["content"] = decision.Content,
            ["date_from"] = decision.DateFrom,
            ["date_to"] = decision.DateTo,
            ["semantic_query"] = decision.SemanticQuery,
        })]);
    }
}
