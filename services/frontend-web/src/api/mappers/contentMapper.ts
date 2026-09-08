import type { ContentResponse } from "../contracts/content/ContentResponse";
import type { Content } from "../../domain/content/Content";

export function mapContentResponse(response: ContentResponse): Content {
  return {
    id: response.id,
    title: response.summary.title,
    source: response.sourceType.toLowerCase() === "youtube" ? "youtube" : "article",
    summary: response.summary.shortSummary,
    keyPoints: response.summary.keyPoints,
    createdAt: new Date(response.createdAtUtc),
  };
}
