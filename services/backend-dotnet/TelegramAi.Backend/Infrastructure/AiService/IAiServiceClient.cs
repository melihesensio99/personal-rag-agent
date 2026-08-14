using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Api.Contracts.Summaries;

namespace TelegramAi.Backend.Infrastructure.AiService;

public interface IAiServiceClient
{
    Task<AiServiceHealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<CreateSummaryResponse> CreateSummaryAsync(
        CreateSummaryRequest request,
        CancellationToken cancellationToken);
}
