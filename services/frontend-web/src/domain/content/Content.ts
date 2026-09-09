export type ContentSource = 'youtube' | 'web';

export interface ReaderBlock {
  type: 'heading' | 'paragraph' | 'image' | 'list' | 'quote';
  text?: string;
  level?: number;
  url?: string;
  caption?: string;
  items?: string[];
}

export interface Content {
  id: string;
  source: ContentSource;
  rawText: string;
  originalUrl?: string;
  imageUrl?: string;
  readerBlocks: ReaderBlock[];
  title: string;
  summary: string;
  keyPoints: string[];
  tags: string[];
  createdAt: Date;
}
