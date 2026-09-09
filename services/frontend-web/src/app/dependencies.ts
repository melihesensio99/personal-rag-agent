import { FetchHttpClient } from '../api/http/HttpClient';
import { ApiContentRepository } from '../api/repositories/ApiContentRepository';
import { CreateContentUseCase } from '../features/quick-capture/application/CreateContentUseCase';

const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';
const httpClient = new FetchHttpClient(apiUrl);

export const createContentUseCase = new CreateContentUseCase(new ApiContentRepository(httpClient));
