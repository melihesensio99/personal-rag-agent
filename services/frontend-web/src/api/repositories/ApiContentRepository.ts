import type { Content, ContentSource } from '../../domain/content/Content';
import type { ContentRepository } from '../../domain/content/ContentRepository';
import type { ContentChunk } from '../../domain/content/ContentChunk';
import type { ContentChunkResponse } from '../contracts/content/ContentChunkResponse';
import type { CreateContentRequest } from '../contracts/content/CreateContentRequest';
import type { ContentResponse } from '../contracts/content/ContentResponse';
import type { PagedContentResponse } from '../contracts/content/PagedContentResponse';
import type { HttpClient } from '../http/HttpClient';
import { mapContentResponse } from '../mappers/contentMapper';

export class ApiContentRepository implements ContentRepository {
  public constructor(private readonly httpClient: HttpClient) {}

  public async list(): Promise<Content[]> {
    const response = await this.httpClient.get<PagedContentResponse>('/api/v1/contents?page=1&pageSize=100');
    return response.items.map(mapContentResponse);
  }

  public async getById(id: string): Promise<Content> {
    const response = await this.httpClient.get<ContentResponse>(`/api/v1/contents/${id}`);
    return mapContentResponse(response);
  }

  public async getChunks(id: string): Promise<ContentChunk[]> {
    const response = await this.httpClient.get<ContentChunkResponse[]>(`/api/v1/contents/${id}/chunks`);
    return response.map((chunk) => ({
      id: chunk.id,
      contentItemId: chunk.contentItemId,
      index: chunk.index,
      text: chunk.text,
      charStart: chunk.charStart,
      charEnd: chunk.charEnd,
      hasEmbedding: chunk.hasEmbedding,
      createdAt: new Date(chunk.createdAtUtc),
    }));
  }

  public async createFromUrl(url: string, source: ContentSource): Promise<Content> {
    const request: CreateContentRequest = {
      text: url,
      sourceType: source === 'youtube' ? 'YouTube' : 'Article',
    };
    const response = await this.httpClient.post<CreateContentRequest, ContentResponse>('/api/v1/contents', request);
    return mapContentResponse(response);
  }

  public delete(id: string): Promise<void> {
    return this.httpClient.delete(`/api/v1/contents/${id}`);
  }
}
