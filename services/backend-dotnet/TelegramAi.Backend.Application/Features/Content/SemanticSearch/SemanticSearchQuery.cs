using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SemanticSearchQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<IReadOnlyList<SemanticSearchChunkResult>>;
