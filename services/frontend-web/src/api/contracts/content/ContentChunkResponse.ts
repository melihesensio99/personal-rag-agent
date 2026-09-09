export interface ContentChunkResponse {
  id: string;
  contentItemId: string;
  index: number;
  text: string;
  charStart: number;
  charEnd: number;
  hasEmbedding: boolean;
  createdAtUtc: string;
}
