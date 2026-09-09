import type { Content } from '../../../domain/content/Content';
import type { ContentRepository } from '../../../domain/content/ContentRepository';

export class ListContentsUseCase {
  public constructor(private readonly repository: ContentRepository) {}
  public execute(): Promise<Content[]> { return this.repository.list(); }
}
