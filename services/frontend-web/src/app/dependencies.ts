import { FetchHttpClient } from "../api/http/HttpClient";
import { ApiContentRepository } from "../api/repositories/ApiContentRepository";
import { CreateSummaryUseCase } from "../features/quick-summary/application/CreateSummaryUseCase";
import { DemoAgentRepository } from "../api/repositories/DemoAgentRepository";
import { SendAgentMessageUseCase } from "../features/assistant/application/SendAgentMessageUseCase";
import { DemoLibrarySearchRepository } from "../api/repositories/DemoLibrarySearchRepository";
import { SearchLibraryUseCase } from "../features/library/application/SearchLibraryUseCase";
import { ApiLibrarySearchRepository } from "../api/repositories/ApiLibrarySearchRepository";

const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:8080";
const httpClient = new FetchHttpClient(apiUrl);
const contentRepository = new ApiContentRepository(httpClient);

export const createSummaryUseCase = new CreateSummaryUseCase(contentRepository);
export const sendAgentMessageUseCase = new SendAgentMessageUseCase(new DemoAgentRepository());
export const searchLibraryUseCase = new SearchLibraryUseCase(new ApiLibrarySearchRepository(httpClient));
