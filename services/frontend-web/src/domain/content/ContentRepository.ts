import type { Content, ContentSource } from './Content';

export interface ContentRepository {
  createFromUrl(url: string, source: ContentSource): Promise<Content>;
}
