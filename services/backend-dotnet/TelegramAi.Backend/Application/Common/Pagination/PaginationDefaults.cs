namespace TelegramAi.Backend.Application.Common.Pagination;

public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int page) => Math.Max(DefaultPage, page);
    public static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);
}
