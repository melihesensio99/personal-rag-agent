namespace TelegramAi.Backend.Application.Contracts.Answers;
public sealed record AnswerChunkInput(int Index, string ContentId, string ChunkId, string ContentTitle, string ContentUrl, string SourceType, string ContentKind, int ChunkIndex, string Text, double Distance, double Similarity);
public sealed record CreateAnswerInput(string ContentId, string Question, IReadOnlyList<AnswerChunkInput> Chunks);
public sealed record CreateAnswerResult(string ContentId, string Answer, IReadOnlyList<int> UsedChunkIndexes, string Language, string Provider);
