export interface HttpClient {
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
}

export class FetchHttpClient implements HttpClient {
  public constructor(private readonly baseUrl: string) {}

  public async post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
    } catch {
      throw new Error('Backend servisine ulaşılamadı. Servislerin çalıştığından emin olun.');
    }

    if (!response.ok) {
      const problem = await response.json().catch(() => undefined) as { detail?: string; title?: string } | undefined;
      throw new Error(problem?.detail ?? problem?.title ?? 'İçerik analiz edilemedi. Lütfen tekrar deneyin.');
    }

    return response.json() as Promise<TResponse>;
  }
}
