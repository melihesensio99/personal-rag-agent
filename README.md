# Personal RAG Agent

A learning-oriented product that combines a .NET application backend with a Python AI/RAG service.

The current pipeline separates source extraction, summary generation and RAG
grounding. The original extracted text is retained for chunking and retrieval;
summary-specific compression is applied only to the Mistral summary request.

## Services

- `services/backend-dotnet/TelegramAi.Backend.Api`: product API and orchestration
- `services/ai-service-python`: AI and RAG capabilities
- `compose.yaml`: local PostgreSQL + backend + AI service runtime

## Local development

Run the Python service:

```powershell
cd services/ai-service-python
python -m venv .venv
.venv\Scripts\python -m pip install -e ".[dev]"
.venv\Scripts\python -m uvicorn app.main:app --reload
```

The local cross-encoder reranker is loaded during application startup. Wait for
`Application startup complete` before sending requests. The first startup can
take longer, but subsequent requests reuse the model already held in memory.

Run the .NET backend in another terminal:

```powershell
dotnet run --project services/backend-dotnet/TelegramAi.Backend.Api
```

Check the integrated health endpoint at `http://localhost:5000/api/v1/system/health` using the URL printed by `dotnet run`.

For PostgreSQL-backed persistence, start the database first:

```powershell
docker compose up postgres -d
```

The backend uses the local connection string in [appsettings.json](/C:/Users/melih/Documents/ChatGPT/telegramBot/services/backend-dotnet/TelegramAi.Backend.Api/appsettings.json). Entity Framework Core migrations are applied automatically on startup.

For real Telegram testing, configure the bot token as a local .NET user secret instead of writing it into the repository.

Alternatively, run both services with:

```powershell
docker compose up --build
```

## Current reliability behavior

- Article, PMC, PubMed and YouTube extractors keep the usable source text for
  chunking; web-page noise and source-scope markers are removed where possible.
- Mistral summaries use compact structured JSON: at most three concise key
  points and a 3,200-token output budget. Short content may contain fewer key
  points.
- If Mistral returns truncated/invalid JSON or times out after retries, a
  deterministic fallback summary is returned so ingestion can continue. This
  fallback does not affect chunking, embeddings or RAG retrieval.
- Summary fields are sanitized before reaching the UI, so provider Markdown
  markers such as `**` do not leak into Telegram output.
- RAG answers expose both the context index sent to the answer model and the
  original stored chunk index. Invalid model-reported citation indexes are
  discarded by the backend.

For the end-to-end grounding procedure and the live canary result, see
[docs/RAG_GROUNDING_TEST_GUIDE.md](docs/RAG_GROUNDING_TEST_GUIDE.md).

## Verification

Run the Python service tests with:

```powershell
cd services/ai-service-python
python -m pytest -q -p no:cacheprovider
```

The summary provider has tests for valid structured output, wrapped JSON,
long-input compression, repair retries, provider-failure fallback and
Markdown cleanup.
