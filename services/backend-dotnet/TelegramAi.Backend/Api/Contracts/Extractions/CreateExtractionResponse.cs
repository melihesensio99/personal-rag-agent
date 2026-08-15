namespace TelegramAi.Backend.Api.Contracts.Extractions;

public sealed record CreateExtractionResponse(
    string ContentId,
    string SourceType,
    string ExtractionStatus,
    string? Title,
    string ExtractedText,
    string? OriginalUrl,
    ExtractionMetadataResponse Metadata);
