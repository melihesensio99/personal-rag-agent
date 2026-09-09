import type { Content } from '../../../domain/content/Content';
import type { ContentChunk } from '../../../domain/content/ContentChunk';
import type { ContentRepository } from '../../../domain/content/ContentRepository';

export interface ContentDetail {
  content: Content;
  chunks: ContentChunk[];
}

export class GetContentDetailUseCase {
  public constructor(private readonly contentRepository: ContentRepository) {}

  public async execute(id: string): Promise<ContentDetail> {
    const [content, chunks] = await Promise.all([
      this.contentRepository.getById(id),
      this.contentRepository.getChunks(id),
    ]);

    return { content, chunks };
  }
}
