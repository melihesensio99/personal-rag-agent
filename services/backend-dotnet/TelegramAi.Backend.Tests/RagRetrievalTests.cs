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
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Application.Content.Services;
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

        var result = await service.SemanticAnswerAsync("Kahve kalbe zararlı mı?", 8, null, CancellationToken.None);

        Assert.Single(result.Sources);
        Assert.Equal(coffee.ChunkId, result.Sources[0].ChunkId);
        var answerRequest = Assert.IsType<CreateAnswerRequest>(aiClient.LastAnswerRequest);
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

        var result = await service.SemanticAnswerAsync("Kahve kalbe zararlı mı?", 8, null, CancellationToken.None);

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

        var result = await service.SemanticAnswerDebugAsync(
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

    private static ContentApplicationService CreateService(
        IAiServiceClient aiClient,
        IContentRepository repository)
    {
        return new ContentApplicationService(
            aiClient,
            repository,
            NullLogger<ContentApplicationService>.Instance,
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
    }

    private sealed class StubAiServiceClient(IReadOnlyList<RerankScore> rerankScores) : IAiServiceClient
    {
        public CreateAnswerRequest? LastAnswerRequest { get; private set; }

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

        public Task<RerankResponse> RerankAsync(RerankRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new RerankResponse("test-reranker", rerankScores));
        }

        public Task<CreateAnswerResponse> CreateAnswerAsync(
            CreateAnswerRequest request,
            CancellationToken cancellationToken)
        {
            LastAnswerRequest = request;
            return Task.FromResult(new CreateAnswerResponse(
                request.ContentId,
                "Kayıtlı kahve kaynağına dayalı cevap.",
                [0],
                "tr",
                "test"));
        }

        public Task<AiServiceHealthResponse> GetHealthAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CreateChunksResponse> CreateChunksAsync(CreateChunksRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CreateExtractionResponse> CreateExtractionAsync(CreateExtractionRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ClassifyIntentResponse> ClassifyIntentAsync(ClassifyIntentRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CreateSummaryResponse> CreateSummaryAsync(CreateSummaryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
