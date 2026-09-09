namespace TelegramAi.Backend.Domain.Content;

public sealed record ReaderBlock(
    string Type,
    string? Text = null,
    int? Level = null,
    string? Url = null,
    string? Caption = null);
