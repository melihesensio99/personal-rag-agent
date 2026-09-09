import type { ContentResponse } from '../contracts/content/ContentResponse';
import type { Content } from '../../domain/content/Content';

export function mapContentResponse(response: ContentResponse): Content {
  return {
    id: response.id,
    source: response.sourceType.toLowerCase() === 'youtube' ? 'youtube' : 'web',
    rawText: response.rawText,
    originalUrl: response.originalUrl ?? undefined,
    imageUrl: response.imageUrl ?? undefined,
    title: response.summary.title,
    summary: response.summary.shortSummary,
    keyPoints: response.summary.keyPoints,
    tags: response.summary.tags,
    createdAt: new Date(response.createdAtUtc),
  };
}
