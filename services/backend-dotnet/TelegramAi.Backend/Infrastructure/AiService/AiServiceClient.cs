using System.Net.Http.Json;
using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Chunks;
using TelegramAi.Backend.Api.Contracts.Embeddings;
using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Api.Contracts.Reranking;
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

    public async Task<CreateChunksResponse> CreateChunksAsync(
        CreateChunksRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceCreateChunksRequest(
            ContentId: request.ContentId,
            Text: request.Text,
            ChunkSize: request.ChunkSize,
            Overlap: request.Overlap);

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/chunks",
            aiRequest,
            cancellationToken);

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

        var chunks = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateChunksResponse>(cancellationToken);

        if (chunks is null)
        {
            throw new InvalidOperationException("AI service returned an empty chunk response.");
        }

        return new CreateChunksResponse(
            ContentId: chunks.ContentId,
            ChunkSize: chunks.ChunkSize,
            Overlap: chunks.Overlap,
            TotalChunks: chunks.TotalChunks,
            Chunks: chunks.Chunks
                .Select(chunk => new TextChunkResponse(
                    Index: chunk.Index,
                    Text: chunk.Text,
                    CharStart: chunk.CharStart,
                    CharEnd: chunk.CharEnd))
                .ToList());
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

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

        var extraction = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateExtractionResponse>(cancellationToken);

        if (extraction is null)
        {
            throw new InvalidOperationException(
                "AI service returned an empty extraction response.");
        }

        return new CreateExtractionResponse(
            ContentId: extraction.ContentId,
            SourceType: extraction.SourceType,
            DetectedContentKind: extraction.DetectedContentKind,
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

    public async Task<CreateEmbeddingsResponse> CreateEmbeddingsAsync(
        CreateEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceCreateEmbeddingsRequest(
            ContentId: request.ContentId,
            Texts: request.Texts);

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/embeddings",
            aiRequest,
            cancellationToken);

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

        var embeddings = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateEmbeddingsResponse>(cancellationToken);

        if (embeddings is null)
        {
            throw new InvalidOperationException("AI service returned an empty embedding response.");
        }

        return new CreateEmbeddingsResponse(
            ContentId: embeddings.ContentId,
            Model: embeddings.Model,
            Dimension: embeddings.Dimension,
            Embeddings: embeddings.Embeddings
                .Select(embedding => new TextEmbeddingResponse(
                    Index: embedding.Index,
                    Embedding: embedding.Embedding))
                .ToList());
    }

    public async Task<RerankResponse> RerankAsync(
        RerankRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceRerankRequest(
            request.Query,
            request.Documents.Select(document => new AiServiceRerankDocument(document.Index, document.Text)).ToList());

        var httpResponse = await httpClient.PostAsJsonAsync("/api/v1/rerank", aiRequest, cancellationToken);
        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);
        var response = await httpResponse.Content.ReadFromJsonAsync<AiServiceRerankResponse>(cancellationToken)
            ?? throw new InvalidOperationException("AI service returned an empty rerank response.");

        return new RerankResponse(
            response.Model,
            response.Scores.Select(score => new RerankScore(score.Index, score.Score)).ToList());
    }

    public async Task<CreateAnswerResponse> CreateAnswerAsync(
        CreateAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceCreateAnswerRequest(
            ContentId: request.ContentId,
            Question: request.Question,
            Chunks: request.Chunks
                .Select(chunk => new AiServiceAnswerChunkRequest(
                    Index: chunk.Index,
                    ContentId: chunk.ContentId,
                    ChunkId: chunk.ChunkId,
                    ContentTitle: chunk.ContentTitle,
                    ContentUrl: chunk.ContentUrl,
                    SourceType: chunk.SourceType,
                    ContentKind: chunk.ContentKind,
                    ChunkIndex: chunk.ChunkIndex,
                    Text: chunk.Text,
                    Distance: chunk.Distance,
                    Similarity: chunk.Similarity))
                .ToList());

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/answers",
            aiRequest,
            cancellationToken);

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

        var answer = await httpResponse.Content.ReadFromJsonAsync<AiServiceCreateAnswerResponse>(cancellationToken);

        if (answer is null)
        {
            throw new InvalidOperationException("AI service returned an empty answer response.");
        }

        return new CreateAnswerResponse(
            ContentId: answer.ContentId,
            Answer: answer.Answer,
            UsedChunkIndexes: answer.UsedChunkIndexes,
            Language: answer.Language,
            Provider: answer.Provider);
    }

    public async Task<ClassifyIntentResponse> ClassifyIntentAsync(
        ClassifyIntentRequest request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiServiceClassifyIntentRequest(
            Message: request.Message,
            CurrentDate: request.CurrentDate);

        var httpResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/intents",
            aiRequest,
            cancellationToken);

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

        var intent = await httpResponse.Content.ReadFromJsonAsync<AiServiceClassifyIntentResponse>(cancellationToken);

        if (intent is null)
        {
            throw new InvalidOperationException("AI service returned an empty intent response.");
        }

        return new ClassifyIntentResponse(
            Action: intent.Action,
            Intent: intent.Intent,
            Query: intent.Query,
            Content: intent.Content,
            ContentKind: intent.ContentKind,
            SourceType: intent.SourceType,
            TimeFilter: intent.TimeFilter,
            DateFrom: intent.DateFrom,
            DateTo: intent.DateTo,
            SemanticQuery: intent.SemanticQuery,
            Keywords: intent.Keywords,
            NeedsClarification: intent.NeedsClarification,
            ClarificationMessage: intent.ClarificationMessage);
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

        await EnsureSuccessWithDetailsAsync(httpResponse, cancellationToken);

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

    private static async Task EnsureSuccessWithDetailsAsync(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken)
    {
        if (httpResponse.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"AI service request failed with status code {(int)httpResponse.StatusCode} ({httpResponse.StatusCode}). Response: {responseBody}");
    }
}
