using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SemanticSearchDebugQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticSearchDebugResult>;
