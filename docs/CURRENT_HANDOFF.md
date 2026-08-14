# Current Codex handoff

## Status

Ready for review.

## Implemented vertical slice

The repository now contains a .NET 10 product backend and a Python 3.12 FastAPI AI service with several working slices:

- `GET /api/v1/system/health` on .NET calls `GET /health` on Python through a typed async HTTP client.
- `POST /api/v1/content/summaries` on .NET calls `POST /api/v1/summaries` on Python and returns a structured summary payload.
- `POST /api/v1/contents` on .NET creates a content item, calls the Python summary service, and stores the content plus summary in PostgreSQL through EF Core.
- `GET /api/v1/contents/{id}` on .NET returns the previously stored content item and summary.
- Real Telegram polling is enabled in the .NET backend. Incoming Telegram text messages are routed through the content-creation flow and answered with a formatted summary message.
- The backend now contains an EF Core + PostgreSQL persistence layer with an initial migration for `contents`.

The summarization path currently uses a deterministic fake provider plus a real system prompt file. No external LLM API key has been introduced yet. This keeps the prompt, contract and service boundary stable before adding a paid model provider.

## Files to review

- `services/backend-dotnet/TelegramAi.Backend/Program.cs`
- `services/backend-dotnet/TelegramAi.Backend/Api/`
- `services/backend-dotnet/TelegramAi.Backend/Application/`
- `services/backend-dotnet/TelegramAi.Backend/Domain/`
- `services/backend-dotnet/TelegramAi.Backend/Infrastructure/`
- `services/backend-dotnet/TelegramAi.Backend/Infrastructure/Telegram/`
- `services/backend-dotnet/TelegramAi.Backend/Infrastructure/Persistence/`
- `services/ai-service-python/app/`
- `services/ai-service-python/app/prompts/content_summary_v1.txt`
- `services/ai-service-python/app/services/summary_service.py`
- `services/ai-service-python/tests/`
- `contracts/ai-service-health.schema.json`
- `contracts/ai-service-summary.schema.json`
- `compose.yaml`
- `docs/ARCHITECTURE.md`

## Important decisions

- .NET owns product orchestration; Python owns AI-oriented summarization behavior.
- Services communicate through typed HTTP/JSON rather than sharing implementation details.
- The backend API contract is allowed to stay idiomatic for .NET while the AI service contract uses snake_case; explicit mapping now bridges the boundary.
- The backend now contains a minimal `Application + Domain + Infrastructure` split so content persistence can evolve without pushing orchestration into endpoints.
- Persistence now uses PostgreSQL through EF Core rather than the old in-memory repository.
- Telegram is explicitly treated as a .NET-side inbound product channel. The real Telegram Bot API polling integration is now wired and working.
- PostgreSQL is now the intended primary persistence store and EF Core migrations are configured to run on backend startup.
- Configuration validation and a five-second HTTP timeout remain in place after the refactor.
- The Python response forbids undeclared fields to keep the service boundary explicit.
- A real prompt file exists before any real model integration so prompt versioning can start early.
- The LLM integration is still deliberately deferred until the local service boundary and output shape are verified.

## Verification completed

- `.NET`: `dotnet build ...` completed with zero warnings and zero errors.
- `.NET`: after adding EF Core/Npgsql, `dotnet build ...` completed successfully once NuGet restore was allowed.
- `Python`: `python -m pytest` completed with two passing tests. The installed FastAPI/Starlette combination still emits a TestClient deprecation warning; it does not fail the tests.
- `Docker`: `docker compose config` succeeded. Docker also reported that the current user's Docker config file is inaccessible; compose syntax was still validated.
- End to end: both services were started locally and the flow `POST /api/v1/contents` -> summary generation -> PostgreSQL persistence -> `GET /api/v1/contents/{id}` completed successfully.
- End to end: real Telegram polling was tested on August 14, 2026. The bot `@personal_rag_agent_bot` accepted a user message and returned the formatted summary response produced through the .NET -> Python flow.

## Reviewer focus

- Service boundary and contract consistency
- Whether content creation and content retrieval should move behind dedicated application request/response models before the next feature
- Whether the real Telegram integration should start with polling or webhook mode after the user creates a bot token
- Summary request/response naming and future versioning strategy
- Configuration validation
- Timeout and cancellation behavior
- Whether failure responses should be normalized in the next slice
- Whether the fake provider logic should move behind an explicit provider abstraction before adding OpenAI
- Python package/test organization
- Docker build context and health check

Do not edit files. Report findings and teach the concepts using `docs/REVIEWER_INSTRUCTIONS.md`.
