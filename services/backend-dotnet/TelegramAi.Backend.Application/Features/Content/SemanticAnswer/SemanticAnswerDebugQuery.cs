using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed record SemanticAnswerDebugQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticAnswerDebugResult>;

public sealed class SemanticAnswerDebugQueryHandler(ISemanticAnswerService service)
    : IRequestHandler<SemanticAnswerDebugQuery, SemanticAnswerDebugResult>
{
    public Task<SemanticAnswerDebugResult> Handle(SemanticAnswerDebugQuery request, CancellationToken cancellationToken) =>
        service.AnswerDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}
