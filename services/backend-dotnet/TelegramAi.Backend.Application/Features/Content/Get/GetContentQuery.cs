using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Get;

public sealed record GetContentQuery(Guid Id) : IRequest<ContentItem?>;
