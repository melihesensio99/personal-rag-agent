using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed record SemanticAnswerDebugQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticAnswerDebugResult>;
