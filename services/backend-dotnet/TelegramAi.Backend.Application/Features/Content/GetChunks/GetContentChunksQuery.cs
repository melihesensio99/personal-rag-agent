using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.GetChunks;

public sealed record GetContentChunksQuery(Guid Id) : IRequest<IReadOnlyList<ContentChunk>>;
