import type { ContentResponse } from "./ContentResponse";
export interface PagedContentResponse { items: ContentResponse[]; page: number; pageSize: number; totalCount: number; totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean; }
