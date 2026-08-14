using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TelegramAi.Backend.Infrastructure.Telegram.TelegramApi;

public interface ITelegramBotApiClient
{
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long? offset,
        int timeoutSeconds,
        CancellationToken cancellationToken);

    Task SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken);
}

public sealed class TelegramBotApiClient(
    HttpClient httpClient,
    IOptions<TelegramBotOptions> options) : ITelegramBotApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TelegramBotOptions _options = options.Value;
    private readonly string _botApiBaseUrl = $"https://api.telegram.org/bot{options.Value.BotToken}/";

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long? offset,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var query = new StringBuilder($"{_botApiBaseUrl}getUpdates?allowed_updates=%5B%22message%22%5D");
        query.Append("&timeout=");
        query.Append(timeoutSeconds);

        if (offset.HasValue)
        {
            query.Append("&offset=");
            query.Append(offset.Value);
        }

        using var response = await httpClient.GetAsync(query.ToString(), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TelegramApiEnvelope<IReadOnlyList<TelegramUpdate>>>(
            SerializerOptions,
            cancellationToken);

        if (payload is null || !payload.Ok)
        {
            return [];
        }

        return payload.Result ?? [];
    }

    public async Task SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        var payload = new TelegramSendMessageRequest(chatId, text);

        using var response = await httpClient.PostAsJsonAsync(
            $"{_botApiBaseUrl}sendMessage",
            payload,
            SerializerOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

public sealed record TelegramApiEnvelope<T>(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] T? Result);

public sealed record TelegramUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessage? Message);

public sealed record TelegramMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("chat")] TelegramChat Chat,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("from")] TelegramUser? From);

public sealed record TelegramChat(
    [property: JsonPropertyName("id")] long Id);

public sealed record TelegramUser(
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("username")] string? Username);

public sealed record TelegramSendMessageRequest(
    [property: JsonPropertyName("chat_id")] long ChatId,
    [property: JsonPropertyName("text")] string Text);
