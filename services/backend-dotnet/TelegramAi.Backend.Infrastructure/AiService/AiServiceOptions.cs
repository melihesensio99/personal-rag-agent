using System.ComponentModel.DataAnnotations;

namespace TelegramAi.Backend.Infrastructure.AiService;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    [Required, Url]
    public required Uri BaseUrl { get; init; }

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 5;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
