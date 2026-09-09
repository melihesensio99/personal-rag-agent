using System.ComponentModel.DataAnnotations;

namespace TelegramAi.Backend.Infrastructure.AiService;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    [Required, Url]
    public required Uri BaseUrl { get; init; }

    [Range(1, 180)]
    public int TimeoutSeconds { get; init; } = 120;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
