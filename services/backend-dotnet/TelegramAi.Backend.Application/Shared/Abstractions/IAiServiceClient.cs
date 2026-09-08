using TelegramAi.Backend.Application.Contracts.Health;
using TelegramAi.Backend.Application.Contracts.Intents;
using TelegramAi.Backend.Application.Contracts.Answers;
using TelegramAi.Backend.Application.Contracts.Chunks;
using TelegramAi.Backend.Application.Contracts.Embeddings;
using TelegramAi.Backend.Application.Contracts.Extractions;
using TelegramAi.Backend.Application.Contracts.Reranking;
using TelegramAi.Backend.Application.Contracts.Summaries;

namespace TelegramAi.Backend.Application.Shared.Abstractions;

public interface IAiServiceClient
{
    Task<AiServiceHealthResult> GetHealthAsync(CancellationToken cancellationToken);
    Task<CreateChunksResult> CreateChunksAsync(CreateChunksInput request, CancellationToken cancellationToken);
    Task<CreateExtractionResult> CreateExtractionAsync(CreateExtractionInput request, CancellationToken cancellationToken);
    Task<CreateEmbeddingsResult> CreateEmbeddingsAsync(CreateEmbeddingsInput request, CancellationToken cancellationToken);
    Task<RerankResult> RerankAsync(RerankInput request, CancellationToken cancellationToken);
    Task<CreateAnswerResult> CreateAnswerAsync(CreateAnswerInput request, CancellationToken cancellationToken);
    Task<ClassifyIntentResult> ClassifyIntentAsync(ClassifyIntentInput request, CancellationToken cancellationToken);
    Task<CreateSummaryResult> CreateSummaryAsync(CreateSummaryInput request, CancellationToken cancellationToken);
}
