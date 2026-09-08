export type LibrarySourceType = "youtube" | "article";
export interface LibrarySource { id: string; title: string; sourceType: LibrarySourceType; summary: string; durationLabel: string; createdAtLabel: string; }
