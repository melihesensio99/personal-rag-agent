namespace TelegramAi.Backend.Application.Contracts.Extractions;

public sealed record CreateExtractionInput(string ContentId, string? SourceType, string? Url, string? Text);
public sealed record ExtractionMetadataResult(string? Domain, string? ContentType, string? FinalUrl, IReadOnlyDictionary<string, object?> Extra);
public sealed record CreateExtractionResult(string ContentId, string SourceType, string DetectedContentKind, string ExtractionStatus, string? Title, string ExtractedText, string? OriginalUrl, ExtractionMetadataResult Metadata);
