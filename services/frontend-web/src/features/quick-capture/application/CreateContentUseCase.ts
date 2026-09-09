import type { Content, ContentSource } from '../../../domain/content/Content';
import type { ContentRepository } from '../../../domain/content/ContentRepository';

export class CreateContentUseCase {
  public constructor(private readonly repository: ContentRepository) {}

  public execute(url: string, source: ContentSource): Promise<Content> {
    const normalizedUrl = url.trim();
    try {
      new URL(normalizedUrl);
    } catch {
      throw new Error('Geçerli bir kaynak bağlantısı girin.');
    }
    return this.repository.createFromUrl(normalizedUrl, source);
  }
}
