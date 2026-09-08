namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record CreateAnswerRequest(
    string ContentId,
    string Question,
    IReadOnlyList<CreateAnswerChunkRequest> Chunks);
