using TelegramAi.Backend.Application.Shared.Common.Pagination;

namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed class ListContentsRequest
{
    public string? Search { get; init; }
    public string? SourceType { get; init; }
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
    public int Page { get; init; } = PaginationDefaults.DefaultPage;
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}
