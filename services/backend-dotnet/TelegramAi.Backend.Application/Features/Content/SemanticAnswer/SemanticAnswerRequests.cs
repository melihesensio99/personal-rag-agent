using MediatR;
using TelegramAi.Backend.Application.Features.Content.Services;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed record SemanticAnswerRequest(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticAnswerResult>;

public sealed record SemanticAnswerDebugRequest(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticAnswerDebugResult>;

public sealed class SemanticAnswerRequestHandler(IContentApplicationService service)
    : IRequestHandler<SemanticAnswerRequest, SemanticAnswerResult>
{
    public Task<SemanticAnswerResult> Handle(SemanticAnswerRequest request, CancellationToken cancellationToken) =>
        service.SemanticAnswerAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}

public sealed class SemanticAnswerDebugRequestHandler(IContentApplicationService service)
    : IRequestHandler<SemanticAnswerDebugRequest, SemanticAnswerDebugResult>
{
    public Task<SemanticAnswerDebugResult> Handle(SemanticAnswerDebugRequest request, CancellationToken cancellationToken) =>
        service.SemanticAnswerDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}
