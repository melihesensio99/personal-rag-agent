using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Api.Contracts.Summaries;

namespace TelegramAi.Backend.Infrastructure.AiService;

public interface IAiServiceClient
{
    Task<AiServiceHealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<CreateExtractionResponse> CreateExtractionAsync(
        CreateExtractionRequest request,
        CancellationToken cancellationToken);
    Task<ClassifyIntentResponse> ClassifyIntentAsync(
        ClassifyIntentRequest request,
        CancellationToken cancellationToken);
    Task<CreateSummaryResponse> CreateSummaryAsync(
        CreateSummaryRequest request,
        CancellationToken cancellationToken);
}
