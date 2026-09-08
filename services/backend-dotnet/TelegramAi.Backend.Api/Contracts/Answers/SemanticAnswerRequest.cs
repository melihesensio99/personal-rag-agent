namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record SemanticAnswerRequest(
    string Query,
    int MaxResults = 5,
    Guid? ContentId = null);
