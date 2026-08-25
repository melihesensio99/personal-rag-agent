# Manual Test Flow

This document shows the current end-to-end test flow for the personal RAG agent project.

## 1. Start Local Dependencies

Start PostgreSQL:

```powershell
cd C:\Users\melih\Documents\ChatGPT\telegramBot
docker compose up postgres -d
```

## 2. Start Python AI Service

```powershell
cd C:\Users\melih\Documents\ChatGPT\telegramBot\services\ai-service-python
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Check health:

```text
GET http://127.0.0.1:8000/health
```

## 3. Start .NET Backend

```powershell
cd C:\Users\melih\Documents\ChatGPT\telegramBot
dotnet run --project services/backend-dotnet/TelegramAi.Backend
```

Use the backend URL printed by `dotnet run`. In the current Postman environment the default is:

```text
http://localhost:51524
```

## 4. Postman Flow

Import these files:

```text
C:\Users\melih\Documents\ChatGPT\telegramBot\postman\personal-rag-agent.local.postman_environment.json
C:\Users\melih\Documents\ChatGPT\telegramBot\postman\personal-rag-agent.postman_collection.json
```

Suggested request order:

```text
01 - AI Service / Health
01 - AI Service / Extraction - Auto detect source/content kind
01 - AI Service / Intent - Search request
01 - AI Service / Summary - Direct text
02 - Backend / Health
02 - Backend / System Health (backend + ai-service)
02 - Backend / Create Content - Manual text
02 - Backend / Get Content By Id
02 - Backend / Get Content Chunks By Content Id
02 - Backend / Semantic Search - Query chunks
02 - Backend / Semantic Answer - Query and answer
```

## 5. Telegram Flow

Send `/start` to the bot.

Expected response:

```text
Merhaba. Bana bir metin veya link aciklamasi gonder, ben onu kaydedip ozetleyeyim.
```

Then test these inputs:

```text
RAG ile semantic search arasindaki farki kisa anlat.
```

Expected behavior:

```text
The bot saves the text and returns a Turkish title, summary, key points and tags.
```

Test an article URL:

```text
https://azure.microsoft.com/tr-tr/resources/cloud-computing-dictionary/what-is-retrieval-augmented-generation-rag
```

Expected behavior:

```text
Python uses ArticleExtractor.
If trafilatura is installed, article_parser is trafilatura.
Mistral summarizes the cleaned article text when AI_SERVICE_SUMMARY_PROVIDER=mistral.
.NET stores the content in PostgreSQL.
.NET asks Python for chunk embeddings and stores them in PostgreSQL/pgvector.
Telegram returns the formatted summary.
```

Test a YouTube URL:

```text
https://www.youtube.com/watch?v=LeUUxLRdvho
```

Expected behavior:

```text
Python uses YouTubeExtractor.
It reads title, channel and thumbnail metadata.
If subtitles/transcript are available, transcript_status is completed.
Mistral summarizes the extracted metadata plus transcript text when AI_SERVICE_SUMMARY_PROVIDER=mistral.
```

Test a Google search URL:

```text
https://www.google.com/search?q=RAG+Mimarisi
```

Expected behavior:

```text
Python returns extraction_status = unsupported and reason = search_result_page.
.NET does not save it.
Telegram asks the user to send the actual article, video or PDF URL.
```

Test search intent:

```text
bugun attigim videolari listele
```

Expected behavior:

```text
Python intent provider classifies this as search.
.NET queries PostgreSQL by content kind, source type and date filter.
Telegram lists matching records.
```

Test semantic search in Postman after saving an article:

```text
POST http://127.0.0.1:5080/api/v1/search/semantic
```

Body:

```json
{
  "query": "Kas yapmak için günlük ne kadar protein almalıyım?",
  "maxResults": 5
}
```

Expected behavior:

```text
.NET asks Python for a query embedding.
PostgreSQL/pgvector compares the query embedding with stored chunk embeddings.
Response returns the closest chunks. Lower distance means more similar.
This is retrieval only; natural answer generation is the next step.
```

Test semantic answer in Postman after saving an article:

```text
POST http://127.0.0.1:5080/api/v1/search/answer
```

Body:

```json
{
  "query": "Kas yapmak için günlük ne kadar protein almalıyım?",
  "maxResults": 5
}
```

Expected behavior:

```text
.NET asks Python for a query embedding.
PostgreSQL/pgvector returns the closest chunks.
Python answer provider writes a Turkish answer using only those chunks.
Response returns the answer plus the source chunks.
```

## 6. Current Important Limits

The system is not full RAG yet.

Currently implemented:

```text
content ingestion
article extraction
YouTube metadata extraction
YouTube transcript extraction when available
Mistral/Gemini/fake provider abstraction
Mistral summary when configured
Mistral intent classification when configured
PostgreSQL persistence
chunking
embeddings
pgvector semantic search
basic filtered search
```

Not implemented yet:

```text
PDF file upload extraction
image OCR or image understanding
semantic search wired into Telegram natural question flow
```

## 7. Useful Debug Points

Python:

```text
app/services/extraction_service.py
app/services/extractors/article_extractor.py
app/services/extractors/youtube_extractor.py
app/services/extractors/youtube_transcript_provider.py
app/services/summary_providers/gemini_summary_provider.py
app/services/intent_providers/gemini_intent_provider.py
```

.NET:

```text
Application/Telegram/Services/TelegramMessageApplicationService.cs
Application/Content/Services/ContentApplicationService.cs
Infrastructure/AiService/AiServiceClient.cs
Infrastructure/Telegram/TelegramPollingHostedService.cs
Infrastructure/Persistence/EFCoreContentRepository.cs
```
