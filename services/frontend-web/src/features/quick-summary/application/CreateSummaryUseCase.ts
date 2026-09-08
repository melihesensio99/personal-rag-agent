import type { Content, ContentSource } from "../../../domain/content/Content";
import type { ContentRepository } from "../../../domain/content/ContentRepository";

export class CreateSummaryUseCase {
  public constructor(private readonly contentRepository: ContentRepository) {}

  public execute(url: string, source: ContentSource): Promise<Content> {
    const normalizedUrl = url.trim();
    if (!normalizedUrl.startsWith("http")) throw new Error("Geçerli bir bağlantı girin.");
    return this.contentRepository.createFromUrl(normalizedUrl, source);
  }
}
