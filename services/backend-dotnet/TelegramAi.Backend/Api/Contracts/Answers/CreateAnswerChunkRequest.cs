namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record CreateAnswerChunkRequest(
    int Index,
    string ContentId,
    string ChunkId,
    string ContentTitle,
    string ContentUrl,
    string SourceType,
    string ContentKind,
    int ChunkIndex,
    string Text,
    double Distance,
    double Similarity);
