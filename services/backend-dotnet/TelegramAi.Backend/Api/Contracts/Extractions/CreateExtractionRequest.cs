namespace TelegramAi.Backend.Api.Contracts.Extractions;

public sealed record CreateExtractionRequest(
    string ContentId,
    string? SourceType,
    string? Url,
    string? Text);
