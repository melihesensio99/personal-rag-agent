# Architecture

## Current vertical slice

```text
Client
  -> .NET backend
      -> application/domain flow
      -> typed HTTP client
          -> Python FastAPI AI service
```

The .NET service owns product orchestration. The Python service owns AI and RAG capabilities. They communicate through versioned HTTP/JSON contracts.

## Planned ownership

### .NET backend

- Telegram integration
- User, content and reminder workflows
- Content persistence and retrieval
- Authorization
- Application database ownership
- Job orchestration

### Python AI service

- Transcription and media analysis
- Prompts and structured LLM output
- Chunking and embeddings
- Retrieval, reranking and RAG evaluation

Each service owns its implementation and persistence schema. Cross-service access happens through explicit contracts rather than another service's tables.

## Current Telegram direction

Telegram belongs to the .NET backend, not the Python AI service. The bot is treated as an inbound product channel. Incoming Telegram text is converted into a normal content-creation flow, then the Python service is called for summarization.
