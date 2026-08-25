using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Chunks;
using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Embeddings;
using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Api.Contracts.Summaries;

namespace TelegramAi.Backend.Infrastructure.AiService;

public interface IAiServiceClient
{
    Task<AiServiceHealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<CreateChunksResponse> CreateChunksAsync(
        CreateChunksRequest request,
        CancellationToken cancellationToken);
    Task<CreateExtractionResponse> CreateExtractionAsync(
        CreateExtractionRequest request,
        CancellationToken cancellationToken);
    Task<CreateEmbeddingsResponse> CreateEmbeddingsAsync(
        CreateEmbeddingsRequest request,
        CancellationToken cancellationToken);
    Task<CreateAnswerResponse> CreateAnswerAsync(
        CreateAnswerRequest request,
        CancellationToken cancellationToken);
    Task<ClassifyIntentResponse> ClassifyIntentAsync(
        ClassifyIntentRequest request,
        CancellationToken cancellationToken);
    Task<CreateSummaryResponse> CreateSummaryAsync(
        CreateSummaryRequest request,
        CancellationToken cancellationToken);
}
