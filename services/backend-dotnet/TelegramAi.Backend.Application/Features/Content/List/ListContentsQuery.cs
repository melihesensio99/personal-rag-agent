using MediatR;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.List;

public sealed record ListContentsQuery(
    string? Search,
    ContentSourceType? SourceType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page,
    int PageSize) : IRequest<PagedResult<ContentItem>>;
