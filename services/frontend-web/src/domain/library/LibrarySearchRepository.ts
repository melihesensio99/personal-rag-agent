import type { LibrarySource, LibrarySourceType } from "./LibrarySource";
export interface LibrarySearchResult { message: string; sources: LibrarySource[]; appliedType?: LibrarySourceType; }
export interface LibrarySearchRepository { getAll(type?: LibrarySourceType): Promise<LibrarySource[]>; search(command: string): Promise<LibrarySearchResult>; }
