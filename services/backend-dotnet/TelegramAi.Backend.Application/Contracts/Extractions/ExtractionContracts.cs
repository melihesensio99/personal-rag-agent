namespace TelegramAi.Backend.Application.Contracts.Extractions;

public sealed record CreateExtractionInput(string ContentId, string? SourceType, string? Url, string? Text);
public sealed record ExtractionMetadataResult(string? Domain, string? ContentType, string? FinalUrl, IReadOnlyDictionary<string, object?> Extra);
public sealed record ReaderBlockResult(string Type, string? Text, int? Level, string? Url, string? Caption, IReadOnlyList<string>? Items = null);
public sealed record CreateExtractionResult(string ContentId, string SourceType, string DetectedContentKind, string ExtractionStatus, string? Title, string ExtractedText, string? OriginalUrl, IReadOnlyList<ReaderBlockResult> ReaderBlocks, ExtractionMetadataResult Metadata);
