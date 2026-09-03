using TelegramAi.Backend.Api.Contracts.Intents;

namespace TelegramAi.Backend.Application.Telegram.Agents;

public sealed record AgentPlan(
    string Goal,
    AgentTool Tool,
    ClassifyIntentResponse Decision,
    IReadOnlyList<AgentToolCall> Steps);

public sealed record AgentToolCall(
    AgentTool Tool,
    IReadOnlyDictionary<string, object?> Arguments);

public enum AgentTool
{
    SaveIncomingContent,
    SearchSavedContent,
    AnswerUsingSavedContent,
    AskUserForClarification,
}
