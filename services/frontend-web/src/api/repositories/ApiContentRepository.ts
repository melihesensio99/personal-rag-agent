import type { CreateContentRequest } from "../contracts/content/CreateContentRequest";
import type { ContentResponse } from "../contracts/content/ContentResponse";
import type { HttpClient } from "../http/HttpClient";
import { mapContentResponse } from "../mappers/contentMapper";
import type { Content, ContentSource } from "../../domain/content/Content";
import type { ContentRepository } from "../../domain/content/ContentRepository";

export class ApiContentRepository implements ContentRepository {
  public constructor(private readonly httpClient: HttpClient) {}

  public async createFromUrl(url: string, source: ContentSource): Promise<Content> {
    const request: CreateContentRequest = {
      text: url,
      sourceType: source === "youtube" ? "YouTube" : "Article",
    };
    const response = await this.httpClient.post<CreateContentRequest, ContentResponse>("/api/v1/contents", request);
    return mapContentResponse(response);
  }
}
