using System.ComponentModel.DataAnnotations;

namespace TelegramAi.Backend.Application.Content.Services;

public sealed class AnswerRetrievalOptions
{
    public const string SectionName = "AnswerRetrieval";

    [Range(0, 1)]
    public double MinimumRerankScore { get; init; } = 0.5001;
}
