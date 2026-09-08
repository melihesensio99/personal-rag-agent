using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public sealed record CreateContentCommand(
    string Text,
    ContentSourceType? SourceType,
    string? SummaryInputText = null) : IRequest<ContentItem>;
