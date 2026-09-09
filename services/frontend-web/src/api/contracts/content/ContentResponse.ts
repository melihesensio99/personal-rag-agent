export interface ContentResponse {
  id: string;
  sourceType: string;
  rawText: string;
  createdAtUtc: string;
  originalUrl?: string | null;
  imageUrl?: string | null;
  readerBlocks: Array<{
    type: 'heading' | 'paragraph' | 'image';
    text?: string | null;
    level?: number | null;
    url?: string | null;
    caption?: string | null;
  }>;
  summary: {
    title: string;
    shortSummary: string;
    keyPoints: string[];
    tags: string[];
    language: string;
    provider: string;
  };
}
