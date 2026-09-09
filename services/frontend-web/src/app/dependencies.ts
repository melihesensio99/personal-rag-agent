import { FetchHttpClient } from '../api/http/HttpClient';
import { ApiContentRepository } from '../api/repositories/ApiContentRepository';
import { CreateContentUseCase } from '../features/quick-capture/application/CreateContentUseCase';
import { ListContentsUseCase } from '../features/sources/application/ListContentsUseCase';
import { GetContentDetailUseCase } from '../features/sources/application/GetContentDetailUseCase';

const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';
const httpClient = new FetchHttpClient(apiUrl);
const contentRepository = new ApiContentRepository(httpClient);

export const createContentUseCase = new CreateContentUseCase(contentRepository);
export const listContentsUseCase = new ListContentsUseCase(contentRepository);
export const getContentDetailUseCase = new GetContentDetailUseCase(contentRepository);
