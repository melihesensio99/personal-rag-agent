using System.Net.Http.Json;
using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Infrastructure.AiService.Contracts;

namespace TelegramAi.Backend.Infrastructure.AiService;

public sealed class AiServiceClient(HttpClient httpClient) : IAiServiceClient
{
    public async Task<AiServiceHealthResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = await httpClient.GetFromJsonAsync<AiServiceHealthResponse>(
            "/health",
            cancellationToken);

        return health ?? throw new InvalidOperationException(
            "AI service returned an empty health response.");
    }

    public async Task<CreateExtractionResponse> CreateExtractionAsync(
        CreateExtractionRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceCreateExtractionRequest(
            ContentId: request.ContentId,
            SourceType: request.SourceType,
            Url: request.Url,
            Text: request.Text);

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/extractions",
            aiRequest,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        var extraction = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateExtractionResponse>(cancellationToken);

        if (extraction is null)
        {
            throw new InvalidOperationException(
                "AI service returned an empty extraction response.");
        }

        return new CreateExtractionResponse(
            ContentId: extraction.ContentId,
            SourceType: extraction.SourceType,
            ExtractionStatus: extraction.ExtractionStatus,
            Title: extraction.Title,
            ExtractedText: extraction.ExtractedText,
            OriginalUrl: extraction.OriginalUrl,
            Metadata: new ExtractionMetadataResponse(
                Domain: extraction.Metadata.Domain,
                ContentType: extraction.Metadata.ContentType,
                FinalUrl: extraction.Metadata.FinalUrl,
                Extra: extraction.Metadata.Extra));
    }

    public async Task<CreateSummaryResponse> CreateSummaryAsync(
        CreateSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceCreateSummaryRequest(
            ContentId: request.ContentId,
            Text: request.Text);

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/summaries",
            aiRequest,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        var summary = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateSummaryResponse>(cancellationToken);

        if (summary is null)
        {
            throw new InvalidOperationException(
                "AI service returned an empty summary response.");
        }

        return new CreateSummaryResponse(
            ContentId: summary.ContentId,
            Title: summary.Title,
            ShortSummary: summary.ShortSummary,
            KeyPoints: summary.KeyPoints,
            Tags: summary.Tags,
            Language: summary.Language,
            Provider: summary.Provider);
    }
}
