using MediatR;
namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;
public sealed class SemanticSearchDebugQueryHandler(ISemanticSearchService service) : IRequestHandler<SemanticSearchDebugQuery, SemanticSearchDebugResult>
{ public Task<SemanticSearchDebugResult> Handle(SemanticSearchDebugQuery request, CancellationToken cancellationToken) => service.SearchDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken); }
