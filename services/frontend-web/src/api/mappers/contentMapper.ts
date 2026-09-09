import type { ContentResponse } from '../contracts/content/ContentResponse';
import type { Content } from '../../domain/content/Content';

export function mapContentResponse(response: ContentResponse): Content {
  return {
    id: response.id,
    source: response.sourceType.toLowerCase() === 'youtube' ? 'youtube' : 'web',
    rawText: response.rawText,
    originalUrl: response.originalUrl ?? undefined,
    imageUrl: response.imageUrl ?? undefined,
    readerBlocks: (response.readerBlocks ?? []).map((block) => ({
      type: block.type,
      text: block.text ?? undefined,
      level: block.level ?? undefined,
      url: block.url ?? undefined,
      caption: block.caption ?? undefined,
    })),
    title: response.summary.title,
    summary: response.summary.shortSummary,
    keyPoints: response.summary.keyPoints,
    tags: response.summary.tags,
    createdAt: new Date(response.createdAtUtc),
  };
}
