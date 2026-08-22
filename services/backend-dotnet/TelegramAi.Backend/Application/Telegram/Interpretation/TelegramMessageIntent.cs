using TelegramAi.Backend.Application.Content.Queries;

namespace TelegramAi.Backend.Application.Telegram.Interpretation;

public abstract record TelegramMessageIntent(string OriginalText);

public sealed record SaveContentIntent(string Text) : TelegramMessageIntent(Text);

public sealed record SearchContentsIntent(
    string Text,
    SearchContentsQuery Query) : TelegramMessageIntent(Text);

public sealed record ClarifyContentIntent(string Text) : TelegramMessageIntent(Text);
