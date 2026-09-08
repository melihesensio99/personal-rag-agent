export type ContentSource = "youtube" | "article";

export interface Content {
  id: string;
  title: string;
  source: ContentSource;
  url?: string;
  summary: string;
  keyPoints: string[];
  createdAt: Date;
}
