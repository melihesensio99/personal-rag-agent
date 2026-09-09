import { FetchHttpClient } from '../api/http/HttpClient';
import { ApiContentRepository } from '../api/repositories/ApiContentRepository';
import { CreateContentUseCase } from '../features/quick-capture/application/CreateContentUseCase';
import { ListContentsUseCase } from '../features/sources/application/ListContentsUseCase';
import { GetContentDetailUseCase } from '../features/sources/application/GetContentDetailUseCase';

const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';
const httpClient = new FetchHttpClient(apiUrl);
export const contentRepository = new ApiContentRepository(httpClient);

export interface SemanticAnswerResponse {
  query: string;
  answer: string;
  provider: string;
  usedChunkIndexes: number[];
  sources: Array<{
    contentId: string;
    chunkId: string;
    contentTitle: string;
    contentUrl: string;
    sourceType: string;
    contentKind: string;
    chunkIndex: number;
    chunkText: string;
    distance: number;
    similarity: number;
    contentCreatedAtUtc: string;
  }>;
}

export function askSemanticQuestion(
  query: string,
  contentId?: string | null,
): Promise<SemanticAnswerResponse> {
  return httpClient.post('/api/v1/search/answer', {
    query,
    maxResults: 5,
    contentId: contentId || null,
  });
}

export const createContentUseCase = new CreateContentUseCase(contentRepository);
export const listContentsUseCase = new ListContentsUseCase(contentRepository);
export const getContentDetailUseCase = new GetContentDetailUseCase(contentRepository);
