export interface ContentChunk {
  id: string;
  contentItemId: string;
  index: number;
  text: string;
  charStart: number;
  charEnd: number;
  hasEmbedding: boolean;
  createdAt: Date;
}
