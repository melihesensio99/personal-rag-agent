using MediatR;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Shared.Abstractions;

namespace TelegramAi.Backend.Application.Features.Content.Summary;

public sealed record CreateSummaryCommand(CreateSummaryInput Input) : IRequest<CreateSummaryResult>;
