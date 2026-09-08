using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed record SemanticAnswerQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticAnswerResult>;
