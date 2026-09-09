import type { Content, ContentSource } from './Content';
import type { ContentChunk } from './ContentChunk';

export interface ContentRepository {
  list(): Promise<Content[]>;
  getById(id: string): Promise<Content>;
  getChunks(id: string): Promise<ContentChunk[]>;
  createFromUrl(url: string, source: ContentSource): Promise<Content>;
}
