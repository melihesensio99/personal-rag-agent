using MediatR;
using TelegramAi.Backend.Application.Features.Telegram.Services;

namespace TelegramAi.Backend.Application.Features.Telegram.Process;

public sealed record ProcessTelegramMessageRequest(ProcessTelegramMessageCommand Command)
    : IRequest<ProcessTelegramMessageResult>;

public sealed class ProcessTelegramMessageRequestHandler(ITelegramMessageApplicationService service)
    : IRequestHandler<ProcessTelegramMessageRequest, ProcessTelegramMessageResult>
{
    public Task<ProcessTelegramMessageResult> Handle(
        ProcessTelegramMessageRequest request,
        CancellationToken cancellationToken) =>
        service.ProcessAsync(request.Command, cancellationToken);
}
