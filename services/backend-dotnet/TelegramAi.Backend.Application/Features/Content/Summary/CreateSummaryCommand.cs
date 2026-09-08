using MediatR;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Shared.Abstractions;

namespace TelegramAi.Backend.Application.Features.Content.Summary;

public sealed record CreateSummaryCommand(CreateSummaryInput Input) : IRequest<CreateSummaryResult>;

public sealed class CreateSummaryCommandHandler(IAiServiceClient aiServiceClient)
    : IRequestHandler<CreateSummaryCommand, CreateSummaryResult>
{
    public Task<CreateSummaryResult> Handle(CreateSummaryCommand request, CancellationToken cancellationToken) =>
        aiServiceClient.CreateSummaryAsync(request.Input, cancellationToken);
}
