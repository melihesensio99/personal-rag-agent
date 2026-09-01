# Postman setup

Import these two files into Postman:

- `personal-rag-agent.local.postman_environment.json`
- `personal-rag-agent.postman_collection.json`

Suggested order while debugging:

1. `01 - AI Service / Health`
2. `01 - AI Service / Extraction - Auto detect source/content kind`
3. `01 - AI Service / Intent - Search request`
4. `01 - AI Service / Summary - Direct text`
5. `02 - Backend / Health`
6. `02 - Backend / System Health (backend + ai-service)`
7. `02 - Backend / Create Content - Manual text`
8. `02 - Backend / Get Content By Id`

Default local URLs:

- Backend: `http://localhost:51800`
- AI service: `http://localhost:8000`

If your backend runs on another port, just update `backend_base_url` inside the imported environment.
