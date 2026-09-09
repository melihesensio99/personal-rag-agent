export type ContentSource = 'youtube' | 'web';

export interface Content {
  id: string;
  source: ContentSource;
  rawText: string;
  originalUrl?: string;
  imageUrl?: string;
  title: string;
  summary: string;
  keyPoints: string[];
  tags: string[];
  createdAt: Date;
}
