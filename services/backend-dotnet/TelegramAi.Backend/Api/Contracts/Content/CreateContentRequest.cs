namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed record CreateContentRequest(
    string Text,
    string SourceType);
