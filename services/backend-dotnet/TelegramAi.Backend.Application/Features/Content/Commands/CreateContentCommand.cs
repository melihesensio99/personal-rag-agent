using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Commands;

public sealed record CreateContentCommand(
    string Text,
    ContentSourceType? SourceType,
    string? SummaryInputText = null);
