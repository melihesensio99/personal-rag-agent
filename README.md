# Telegram AI Knowledge Assistant

A learning-oriented product that combines a .NET application backend with a Python AI/RAG service.

## Services

- `services/backend-dotnet/TelegramAi.Backend`: product API and orchestration
- `services/ai-service-python`: AI and RAG capabilities

## Local development

Run the Python service:

```powershell
cd services/ai-service-python
python -m venv .venv
.venv\Scripts\python -m pip install -e ".[dev]"
.venv\Scripts\python -m uvicorn app.main:app --reload
```

Run the .NET backend in another terminal:

```powershell
dotnet run --project services/backend-dotnet/TelegramAi.Backend
```

Check the integrated health endpoint at `http://localhost:5000/api/v1/system/health` using the URL printed by `dotnet run`.

Alternatively, run both services with:

```powershell
docker compose up --build
```

