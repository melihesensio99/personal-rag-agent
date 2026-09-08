export interface ContentResponse {
  id: string;
  sourceType: string;
  rawText: string;
  summary: { title: string; shortSummary: string; keyPoints: string[] };
  createdAtUtc: string;
}
