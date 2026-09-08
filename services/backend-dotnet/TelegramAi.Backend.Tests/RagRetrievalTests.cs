using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Chunks;
using TelegramAi.Backend.Api.Contracts.Embeddings;
using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Api.Contracts.Reranking;
using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Application.Shared.Abstractions;

using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.AiService;
using Xunit;

namespace TelegramAi.Backend.Tests;

public sealed class RagRetrievalTests
{
    [Fact]
    public async Task SemanticAnswer_SendsOnlyRerankerApprovedChunksToAnswerModel()
    {
        var coffee = Result("Kahvenin kalbe etkileri", "Kahve kan basıncı ve kalp sağlığını etkileyebilir.", 0.20);
        var muscle = Result("Maksimum kas artışı", "Direnç egzersizi hipertrofiyi destekler.", 0.21);
        var repository = new StubContentRepository([coffee, muscle]);
        var aiClient = new StubAiServiceClient([new RerankScore(0, 0.82), new RerankScore(1, 0.50)]);
        var service = CreateService(aiClient, repository);

        var result = await service.AnswerAsync("Kahve kalbe zararlı mı?", 8, null, CancellationToken.None);

        Assert.Single(result.Sources);
        Assert.Equal(coffee.ChunkId, result.Sources[0].ChunkId);
        var answerRequest = Assert.IsType<TelegramAi.Backend.Application.Contracts.Answers.CreateAnswerInput>(aiClient.LastAnswerRequest);
        var answerChunk = Assert.Single(answerRequest.Chunks);
        Assert.Equal("Kahvenin kalbe etkileri", answerChunk.ContentTitle);
        Assert.DoesNotContain(answerRequest.Chunks, chunk => chunk.ContentTitle == "Maksimum kas artışı");
    }

    [Fact]
    public async Task SemanticAnswer_DoesNotCallAnswerModelWhenRerankerRejectsEveryChunk()
    {
        var repository = new StubContentRepository([
            Result("Alakasız içerik", "Kas geliştirme teknikleri.", 0.20)
        ]);
        var aiClient = new StubAiServiceClient([new RerankScore(0, 0.50)]);
        var service = CreateService(aiClient, repository);

        var result = await service.AnswerAsync("Kahve kalbe zararlı mı?", 8, null, CancellationToken.None);

        Assert.Empty(result.Sources);
        Assert.Null(aiClient.LastAnswerRequest);
        Assert.Equal("backend", result.Provider);
        Assert.Contains("yeterli bilgi bulunamadı", result.Answer);
    }

    [Fact]
    public async Task SemanticAnswerDebug_ReportsRerankDecisionAndStageTimings()
    {
        var coffee = Result("Kahvenin kalbe etkileri", "Kahve kan basıncı ve kalp sağlığını etkileyebilir.", 0.20);
        var muscle = Result("Maksimum kas artışı", "Direnç egzersizi hipertrofiyi destekler.", 0.21);
        var aiClient = new StubAiServiceClient([
            new RerankScore(0, 0.82),
            new RerankScore(1, 0.50)]);
        var service = CreateService(aiClient, new StubContentRepository([coffee, muscle]));

        var result = await service.AnswerDebugAsync(
            "Kahve kalbe zararlı mı?",
            8,
            null,
            CancellationToken.None);

        Assert.Equal(0.5001, result.MinimumRerankScore);
        Assert.Equal(2, result.RerankCandidates.Count);
        Assert.Equal("selected_for_answer", result.RerankCandidates.Single(item => item.ContentTitle.StartsWith("Kahve")).Decision);
        Assert.Equal("below_rerank_threshold", result.RerankCandidates.Single(item => item.ContentTitle.StartsWith("Maksimum")).Decision);
        Assert.NotNull(result.Timing);
        Assert.Single(result.ContextChunksSentToLlm);
    }

    private static SemanticAnswerService CreateService(
        IAiServiceClient aiClient,
        IContentRepository repository)
    {
        return new SemanticAnswerService(
                aiClient,
                new SemanticSearchService(aiClient, repository),
                new RerankingService(aiClient, Options.Create(new AnswerRetrievalOptions { MinimumRerankScore = 0.5001 })),
                Options.Create(new AnswerRetrievalOptions { MinimumRerankScore = 0.5001 }));
    }

    private static SemanticSearchChunkResult Result(string title, string text, double distance)
    {
        return new SemanticSearchChunkResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            "https://example.com/source",
            ContentSourceType.Article,
            ContentKind.Text,
            0,
            text,
            distance,
            DateTimeOffset.UtcNow);
    }

    private sealed class StubContentRepository(IReadOnlyList<SemanticSearchChunkResult> results) : IContentRepository
    {
        public Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
            SemanticSearchChunksQuery query,
            CancellationToken cancellationToken) => Task.FromResult(results);

        public Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddChunksAsync(IReadOnlyList<ContentChunk> chunks, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(Guid contentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContentItem>> SearchAsync(SearchContentsQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TelegramAi.Backend.Application.Shared.Common.Pagination.PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubAiServiceClient(IReadOnlyList<RerankScore> rerankScores) : IAiServiceClient
    {
        public TelegramAi.Backend.Application.Contracts.Answers.CreateAnswerInput? LastAnswerRequest { get; private set; }

        public Task<CreateEmbeddingsResponse> CreateEmbeddingsAsync(
            CreateEmbeddingsRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CreateEmbeddingsResponse(
                request.ContentId,
                "test-embedding",
                2,
                [new TextEmbeddingResponse(0, [0.1f, 0.2f])]));
        }

        public Task<TelegramAi.Backend.Application.Contracts.Reranking.RerankResult> RerankAsync(TelegramAi.Backend.Application.Contracts.Reranking.RerankInput request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TelegramAi.Backend.Application.Contracts.Reranking.RerankResult("test-reranker", rerankScores.Select(score => new TelegramAi.Backend.Application.Contracts.Reranking.RerankScoreResult(score.Index, score.Score)).ToList()));
        }

        public Task<TelegramAi.Backend.Application.Contracts.Answers.CreateAnswerResult> CreateAnswerAsync(
            TelegramAi.Backend.Application.Contracts.Answers.CreateAnswerInput request,
            CancellationToken cancellationToken)
        {
            LastAnswerRequest = request;
            return Task.FromResult(new TelegramAi.Backend.Application.Contracts.Answers.CreateAnswerResult(
                request.ContentId,
                "Kayıtlı kahve kaynağına dayalı cevap.",
                [0],
                "tr",
                "test"));
        }

        public Task<TelegramAi.Backend.Application.Contracts.Health.AiServiceHealthResult> GetHealthAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TelegramAi.Backend.Application.Contracts.Chunks.CreateChunksResult> CreateChunksAsync(TelegramAi.Backend.Application.Contracts.Chunks.CreateChunksInput request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TelegramAi.Backend.Application.Contracts.Embeddings.CreateEmbeddingsResult> CreateEmbeddingsAsync(TelegramAi.Backend.Application.Contracts.Embeddings.CreateEmbeddingsInput request, CancellationToken cancellationToken) => Task.FromResult(new TelegramAi.Backend.Application.Contracts.Embeddings.CreateEmbeddingsResult(request.ContentId, "test-embedding", 2, [new TelegramAi.Backend.Application.Contracts.Embeddings.TextEmbeddingResult(0, [0.1f, 0.2f])]));
        public Task<TelegramAi.Backend.Application.Contracts.Extractions.CreateExtractionResult> CreateExtractionAsync(TelegramAi.Backend.Application.Contracts.Extractions.CreateExtractionInput request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TelegramAi.Backend.Application.Contracts.Intents.ClassifyIntentResult> ClassifyIntentAsync(TelegramAi.Backend.Application.Contracts.Intents.ClassifyIntentInput request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TelegramAi.Backend.Application.Contracts.Summaries.CreateSummaryResult> CreateSummaryAsync(TelegramAi.Backend.Application.Contracts.Summaries.CreateSummaryInput request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
