using TelegramAi.Backend.Application.Features.Telegram.Agents;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Contracts.Intents;

namespace TelegramAi.Backend.Infrastructure.AiService;

public sealed class AiIntentClassifier(IAiServiceClient aiServiceClient) : IIntentClassifier
{
    public async Task<IntentDecision> ClassifyAsync(string text, CancellationToken cancellationToken)
    {
        var response = await aiServiceClient.ClassifyIntentAsync(
            new ClassifyIntentInput(text, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")), cancellationToken);
        return new IntentDecision(response.Action, response.Intent, response.Query, response.Content,
            response.ContentKind, response.SourceType, response.TimeFilter, response.DateFrom, response.DateTo,
            response.SemanticQuery, response.Keywords, response.NeedsClarification, response.ClarificationMessage);
    }
}
