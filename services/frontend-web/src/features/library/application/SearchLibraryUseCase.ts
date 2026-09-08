import type { LibrarySearchRepository } from "../../../domain/library/LibrarySearchRepository";
import type { LibrarySourceType } from "../../../domain/library/LibrarySource";
export class SearchLibraryUseCase {
  public constructor(private readonly repository: LibrarySearchRepository) {}
  public getAll(type?: LibrarySourceType) { return this.repository.getAll(type); }
  public execute(command: string) { return this.repository.search(command); }
}
