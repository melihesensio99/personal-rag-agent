using MediatR;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Shared.Abstractions;

namespace TelegramAi.Backend.Application.Features.Content.Summary;

public sealed record CreateSummaryRequest(CreateSummaryInput Input) : IRequest<CreateSummaryResult>;

public sealed class CreateSummaryRequestHandler(IAiServiceClient aiServiceClient)
    : IRequestHandler<CreateSummaryRequest, CreateSummaryResult>
{
    public Task<CreateSummaryResult> Handle(CreateSummaryRequest request, CancellationToken cancellationToken) =>
        aiServiceClient.CreateSummaryAsync(request.Input, cancellationToken);
}
