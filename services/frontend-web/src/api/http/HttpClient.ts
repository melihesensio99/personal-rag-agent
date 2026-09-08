export interface HttpClient {
  get<TResponse>(path: string): Promise<TResponse>;
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
}

export class FetchHttpClient implements HttpClient {
  public constructor(private readonly baseUrl: string) {}

  public async get<TResponse>(path: string): Promise<TResponse> {
    const response = await fetch(`${this.baseUrl}${path}`);
    if (!response.ok) throw new Error("İstek tamamlanamadı. Lütfen tekrar deneyin.");
    return response.json() as Promise<TResponse>;
  }

  public async post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error("İstek tamamlanamadı. Lütfen tekrar deneyin.");
    return response.json() as Promise<TResponse>;
  }
}
