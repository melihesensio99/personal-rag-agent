import type { HttpClient } from "../http/HttpClient";
import type { PagedContentResponse } from "../contracts/content/PagedContentResponse";
import { mapContentResponse } from "../mappers/contentMapper";
import type { LibrarySearchRepository, LibrarySearchResult } from "../../domain/library/LibrarySearchRepository";
import type { LibrarySource, LibrarySourceType } from "../../domain/library/LibrarySource";

export class ApiLibrarySearchRepository implements LibrarySearchRepository {
  public constructor(private readonly httpClient: HttpClient) {}
  public async getAll(type?: LibrarySourceType): Promise<LibrarySource[]> {
    const params = new URLSearchParams({ page: "1", pageSize: "100" });
    if (type) params.set("sourceType", type === "youtube" ? "YouTube" : "Article");
    const response = await this.httpClient.get<PagedContentResponse>(`/api/v1/contents?${params}`);
    return response.items.map((item) => { const content = mapContentResponse(item); return { id: content.id, title: content.title, sourceType: content.source, summary: content.summary, durationLabel: content.source === "youtube" ? "Video" : "Okuma", createdAtLabel: new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(content.createdAt) }; });
  }
  public async search(command: string): Promise<LibrarySearchResult> { return { message: "Arama endpointi için filtreleri kullan", sources: await this.getAll(), appliedType: undefined }; }
}
