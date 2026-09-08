using MediatR;
namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;
public sealed class SemanticAnswerDebugQueryHandler(ISemanticAnswerService service) : IRequestHandler<SemanticAnswerDebugQuery, SemanticAnswerDebugResult>
{ public Task<SemanticAnswerDebugResult> Handle(SemanticAnswerDebugQuery request, CancellationToken cancellationToken) => service.AnswerDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken); }
