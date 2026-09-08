namespace TelegramAi.Backend.Application.Features.Telegram.Agents;

public sealed record AgentPlan(
    string Goal,
    AgentTool Tool,
    IntentDecision Decision,
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
