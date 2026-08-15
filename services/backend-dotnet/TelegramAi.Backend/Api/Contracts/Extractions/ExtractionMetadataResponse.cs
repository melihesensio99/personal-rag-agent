namespace TelegramAi.Backend.Api.Contracts.Extractions;

public sealed record ExtractionMetadataResponse(
    string? Domain,
    string? ContentType,
    string? FinalUrl,
    IReadOnlyDictionary<string, object?> Extra);
