# Current Codex handoff

## Status

Ready for review.

## Implemented vertical slice

The repository now contains a .NET 10 product backend and a Python 3.12 FastAPI AI service with two working slices:

- `GET /api/v1/system/health` on .NET calls `GET /health` on Python through a typed async HTTP client.
- `POST /api/v1/content/summaries` on .NET calls `POST /api/v1/summaries` on Python and returns a structured summary payload.
- `POST /api/v1/contents` on .NET creates a content item, calls the Python summary service, and stores the content plus summary in an in-memory repository.
- `GET /api/v1/contents/{id}` on .NET returns the previously stored content item and summary.
- `POST /api/v1/telegram/test-messages` simulates an inbound Telegram message, routes it through the content-creation flow, and returns the created Telegram-sourced content plus summary.

The summarization path currently uses a deterministic fake provider plus a real system prompt file. No external LLM API key has been introduced yet. This keeps the prompt, contract and service boundary stable before adding a paid model provider.

## Files to review

- `services/backend-dotnet/TelegramAi.Backend/Program.cs`
- `services/backend-dotnet/TelegramAi.Backend/Api/`
- `services/backend-dotnet/TelegramAi.Backend/Application/`
- `services/backend-dotnet/TelegramAi.Backend/Domain/`
- `services/backend-dotnet/TelegramAi.Backend/Infrastructure/`
- `services/backend-dotnet/TelegramAi.Backend/Infrastructure/Telegram/`
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
- Persistence is currently an in-memory repository by design; it is a temporary step before PostgreSQL/EF Core.
- Telegram is explicitly treated as a .NET-side inbound product channel. The current slice adds configuration, startup diagnostics, and a simulation endpoint before wiring the real Telegram Bot API client.
- Configuration validation and a five-second HTTP timeout remain in place after the refactor.
- The Python response forbids undeclared fields to keep the service boundary explicit.
- A real prompt file exists before any real model integration so prompt versioning can start early.
- The LLM integration is still deliberately deferred until the local service boundary and output shape are verified.

## Verification completed

- `.NET`: `dotnet build ...` completed with zero warnings and zero errors.
- `Python`: `python -m pytest` completed with two passing tests. The installed FastAPI/Starlette combination still emits a TestClient deprecation warning; it does not fail the tests.
- `Docker`: `docker compose config` succeeded. Docker also reported that the current user's Docker config file is inaccessible; compose syntax was still validated.
- End to end: both services were started locally and the flow `POST /api/v1/contents` -> summary generation -> in-memory storage -> `GET /api/v1/contents/{id}` completed successfully.
- End to end: both services were started locally and `POST /api/v1/telegram/test-messages` produced a Telegram-sourced content record with summary output.

## Reviewer focus

- Service boundary and contract consistency
- Whether content creation and content retrieval should move behind dedicated application request/response models before the next feature
- Whether the real Telegram integration should start with polling or webhook mode after the user creates a bot token
- Summary request/response naming and future versioning strategy
- Configuration validation
- Timeout and cancellation behavior
- Whether failure responses should be normalized in the next slice
- Whether the fake provider logic should move behind an explicit provider abstraction before adding OpenAI
- Whether the in-memory repository should expose list/search methods before PostgreSQL is introduced
- Python package/test organization
- Docker build context and health check

Do not edit files. Report findings and teach the concepts using `docs/REVIEWER_INSTRUCTIONS.md`.
