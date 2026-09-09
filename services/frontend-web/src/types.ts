export interface Author {
  name: string;
  role: string;
  avatarUrl: string;
}

export interface Finding {
  id: string;
  phase: string;
  timestamp: string;
  timeSeconds: number;
  title: string;
  description: string;
  confidence: string;
}

export interface TranscriptLine {
  time: string;
  seconds: number;
  speaker: string;
  text: string;
  highlighted?: boolean;
}

export interface ChunkItem {
  id: string;
  index: number;
  text: string;
  tokens: number;
  charStart: number;
  charEnd: number;
  hasEmbedding: boolean;
  category: string;
}

export interface QAPair {
  id: string;
  question: string;
  timeAgo: string;
  answer: string;
  citation: string;
  citationTimestamp: string;
}

export interface SourceItem {
  id: string;
  type: 'youtube' | 'web' | 'paper';
  title: string;
  originalUrl: string;
  duration: string;
  reliability: number;
  synthesisNumber: string;
  category: string;
  heroImage: string;
  readerBlocks: Array<{
    type: 'heading' | 'paragraph' | 'image' | 'list' | 'quote';
    text?: string;
    level?: number;
    url?: string;
    caption?: string;
    items?: string[];
  }>;
  author: Author;
  executiveSummary: string[];
  findings: Finding[];
  qaPairs: QAPair[];
  transcript: TranscriptLine[];
  chunks: ChunkItem[];
  telemetry: {
    chunksCount: number;
    referencedNamesCount: number;
    wordsCount: string;
    vectorDimensions: number;
    matchPercentage: number;
  };
  tags: string[];
  dateAdded: string;
}

export type NavigationTab =
  | 'hizli-yakala'
  | 'kaynaklarim'
  | 'kaynak-detayi'
  | 'ai-asistan';
