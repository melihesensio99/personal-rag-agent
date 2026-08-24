using Pgvector;

namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentChunk
{
    private ContentChunk()
    {
        Text = string.Empty;
    }

    private ContentChunk(
        Guid id,
        Guid contentItemId,
        int index,
        string text,
        int charStart,
        int charEnd,
        Vector? embedding,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ContentItemId = contentItemId;
        Index = index;
        Text = text;
        CharStart = charStart;
        CharEnd = charEnd;
        Embedding = embedding;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ContentItemId { get; private set; }
    public int Index { get; private set; }
    public string Text { get; private set; }
    public int CharStart { get; private set; }
    public int CharEnd { get; private set; }
    public Vector? Embedding { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ContentChunk Create(
        Guid contentItemId,
        int index,
        string text,
        int charStart,
        int charEnd,
        IReadOnlyList<float>? embedding = null)
    {
        return new ContentChunk(
            Guid.NewGuid(),
            contentItemId,
            index,
            text.Trim(),
            charStart,
            charEnd,
            embedding is null ? null : new Vector(embedding.ToArray()),
            DateTimeOffset.UtcNow);
    }
}
