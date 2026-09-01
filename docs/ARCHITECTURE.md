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

## Current Telegram flow

Telegram belongs to the .NET backend, not the Python AI service. The bot is treated as an inbound product channel. Each message is first sent to the Python intent service. The returned action is one of `save_content`, `list_contents`, `answer_from_memory`, or `ask_clarification`; .NET only executes that action.

For saving, .NET calls extraction, summary and chunk/embedding endpoints, then persists the content and chunks in PostgreSQL. For questions, .NET performs pgvector semantic retrieval and sends the selected chunks to the Python answer provider.

Mistral providers use JSON Schema structured output, Pydantic validation and repair/retry. Python does not override the LLM's semantic action with regex heuristics; it only validates and normalizes technical fields.
