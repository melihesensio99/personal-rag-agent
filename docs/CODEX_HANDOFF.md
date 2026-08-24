# Codex Handoff - Personal RAG Agent

Bu dosya, projeyi başka bir Codex hesabında açınca sohbet geçmişi olmadan devam edebilmek için hazırlanmıştır. Yeni Codex önce bu dosyayı, sonra `docs/ROADMAP.md` ve `docs/MANUAL_TEST_FLOW.md` dosyalarını okumalıdır.

## Project identity

- Repository: `personal-rag-agent`
- Local workspace used during development: `C:\Users\melih\Documents\ChatGPT\telegramBot`
- GitHub remote: `https://github.com/melihesensio99/personal-rag-agent.git`
- Main goal: Telegram üzerinden link/metin kaydedip AI ile özetlemek, içeriği chunk'lara bölmek, sonra RAG ile bu kayıtlar üzerinden soru cevap yapabilmek.

## User learning goal

Kullanıcı backend geliştirici olmak istiyor ve ana uzmanlık alanı `.NET`. AI tarafını “backend geliştiricinin sektörde bilmesi gereken kadar” proje üstünden öğrenmek istiyor. Kodlamayı ağırlıklı Codex yapsın, kullanıcı kodun üstünden öğrenerek ilerlesin istiyor.

İletişim tarzı:

- Türkçe, samimi, “bro” tonunda.
- Önce sonucu söyle, sonra kısa açıklama.
- Kullanıcı beklerken neye bakması gerektiğini söylemek faydalı.
- Kavramları projedeki karşılığıyla anlat: LLM, intent, extraction, summary, chunk, embedding, pgvector, RAG.

## Current architecture

```text
Telegram Bot
→ .NET Backend API / Application layer
→ Python FastAPI AI Service
→ PostgreSQL
```

### .NET backend

Location:

```text
services/backend-dotnet/TelegramAi.Backend
```

Responsibilities:

- Telegram polling.
- Kullanıcı mesajını alır.
- AI intent endpoint ile mesajın `save`, `search`, `clarify` olduğunu öğrenir.
- Save ise content pipeline'ı çalıştırır.
- Search ise PostgreSQL kayıtlarını filtreler.
- PostgreSQL persistence EF Core ile yapılır.
- Telegram response formatting yapar.

Important files:

```text
services/backend-dotnet/TelegramAi.Backend/Infrastructure/Telegram/TelegramPollingHostedService.cs
services/backend-dotnet/TelegramAi.Backend/Application/Telegram/Services/TelegramMessageApplicationService.cs
services/backend-dotnet/TelegramAi.Backend/Application/Content/Services/ContentApplicationService.cs
services/backend-dotnet/TelegramAi.Backend/Infrastructure/AiService/AiServiceClient.cs
services/backend-dotnet/TelegramAi.Backend/Infrastructure/Persistence/EfCoreContentRepository.cs
services/backend-dotnet/TelegramAi.Backend/Domain/Content/ContentItem.cs
services/backend-dotnet/TelegramAi.Backend/Domain/Content/ContentChunk.cs
```

### Python AI service

Location:

```text
services/ai-service-python
```

Responsibilities:

- Intent classification.
- Article extraction.
- YouTube metadata/transcript extraction.
- Summary generation.
- Chunking.
- Provider abstraction for fake/Gemini/Mistral.

Important files:

```text
services/ai-service-python/app/main.py
services/ai-service-python/app/dependencies.py
services/ai-service-python/app/routers/intents.py
services/ai-service-python/app/routers/extractions.py
services/ai-service-python/app/routers/summaries.py
services/ai-service-python/app/routers/chunks.py
services/ai-service-python/app/services/intent_providers/mistral_intent_provider.py
services/ai-service-python/app/services/summary_providers/mistral_summary_provider.py
services/ai-service-python/app/services/extractors/article_extractor.py
services/ai-service-python/app/services/extractors/youtube_extractor.py
services/ai-service-python/app/services/extractors/youtube_transcript_provider.py
services/ai-service-python/app/services/chunking_service.py
```

## Current capabilities

Sistem şu anda şunları yapabiliyor:

- Telegram bot üzerinden metin veya link alabiliyor.
- Mistral ile intent çıkarabiliyor:
  - `save`
  - `search`
  - `clarify`
- YouTube linklerinden metadata ve transcript çekebiliyor.
- Makale linklerinden Trafilatura ağırlıklı temiz text çıkarabiliyor.
- Mistral ile Türkçe summary üretebiliyor:
  - title
  - short_summary
  - key_points
  - tags
  - language
  - provider
- İçeriği PostgreSQL `contents` tablosuna kaydediyor.
- Extract edilen metni chunk'lara bölüp PostgreSQL `content_chunks` tablosuna yazıyor.
- Postman ile content ve chunk kayıtları incelenebiliyor.

## Important decisions made

### .NET + Python mixed architecture

Karar: Ana backend `.NET`, AI service `Python`.

Sebep:

- Kullanıcı .NET backend geliştirici olmak istiyor.
- Python AI ecosystem tarafında extraction, transcript, LLM provider, ileride embedding/RAG için esneklik sağlıyor.
- Bu gerçek hayatta “.NET backend'e Python AI microservice ekleme” deneyimi veriyor.

### Rule-based intent removed

Önceden bazı kararlar regex/rule-based idi. Kullanıcı özellikle “insanların ne yazacağını bilemeyiz, LLM karar versin” dedi. Bu yüzden rule-based intent interpreter kaldırıldı.

Şu an:

```text
Telegram message
→ Python /api/v1/intents
→ .NET bu intent sonucuna göre save/search/clarify yapar
```

### Extraction still deterministic

LLM kullanıcı niyetini belirler; ama bir URL'nin gerçek içeriğini çıkarmak için extractor kullanılır.

Örnek:

```text
YouTube URL
→ youtube extractor
→ oEmbed metadata
→ transcript provider
```

Bu bilinçli bir ayrım:

- LLM niyet/özet/cevap üretir.
- Extractor gerçek web/video/PDF verisini çeker.

### Summary and chunk input separated

Önceden YouTube için şu metin chunk'a da düşüyordu:

```text
Video title: ...
Channel: ...
Original URL: ...
Transcript: ...
```

Bu RAG için temiz değildi. Şimdi:

- Summary input başlık/URL gibi metadata'dan faydalanabilir.
- Chunk input mümkün olduğunca sadece asıl metin/transcript olur.

### Mistral provider active

Gemini denendi ama ücretsiz/limit/stabilite konuşmalarından sonra Mistral provider eklendi.

Aktif provider `.env` ile belirlenir:

```env
AI_SERVICE_SUMMARY_PROVIDER=mistral
AI_SERVICE_INTENT_PROVIDER=mistral
AI_SERVICE_MISTRAL_API_KEY=...
AI_SERVICE_MISTRAL_INTENT_MODEL=ministral-3b-2512
AI_SERVICE_MISTRAL_SUMMARY_MODEL=ministral-8b-2512
AI_SERVICE_MISTRAL_TIMEOUT_SECONDS=20
```

Gerçek API key repo'ya koyulmamalıdır.

### JSON parsing hardened

Mistral bazen strict JSON yerine şuna benzer cevap döndürebiliyor:

```text
Elbette JSON aşağıda:
{ ... }
```

Bu yüzden intent ve summary provider'larında:

- JSON object ayıklama
- retry
- validate/normalize

eklendi.

### .NET AI service timeout increased

Backend config'te AI service timeout 5 saniyeydi. Mistral summary 10-20 saniye sürebildiği için Telegram'da hata oluşuyordu.

Şu an:

```json
"AiService": {
  "BaseUrl": "http://localhost:8000",
  "TimeoutSeconds": 60
}
```

## Current database state

PostgreSQL tables:

```text
contents
content_chunks
__EFMigrationsHistory
```

`contents`:

- raw input
- source type
- content kind
- summary owned object

`content_chunks`:

- content item id
- index
- text
- char start/end
- created at

Important: `content_chunks` currently stores plain text chunks only. This is not yet vector search. pgvector/embedding is the next major RAG step.

## How to run locally

### 1. PostgreSQL

Docker Desktop should be open. PostgreSQL is expected on:

```text
localhost:5432
Database: personal_rag_agent
Username: postgres
Password: postgres
```

### 2. Python AI service

From:

```text
services/ai-service-python
```

Run:

```powershell
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Health:

```text
http://127.0.0.1:8000/health
```

### 3. .NET backend

From Visual Studio or CLI:

```powershell
dotnet run --project services/backend-dotnet/TelegramAi.Backend/TelegramAi.Backend.csproj
```

Known local HTTP port from launch settings:

```text
http://localhost:51524
```

Health:

```text
http://127.0.0.1:51524/health
```

## How to test

Postman files:

```text
postman/personal-rag-agent.postman_collection.json
postman/personal-rag-agent.local.postman_environment.json
```

Useful flows:

1. Python health.
2. Backend health.
3. AI extraction for article.
4. AI extraction for YouTube.
5. AI summary.
6. AI chunks.
7. Backend create content.
8. Backend get content chunks by content id.
9. Telegram bot direct test.

Known good test URLs:

```text
https://health.clevelandclinic.org/hypertrophy
https://health.clevelandclinic.org/water-aerobics
https://www.youtube.com/watch?v=9Ie4FUvtS0w
https://www.youtube.com/watch?v=FJQTT2B-imk
```

## Tests

Python:

```powershell
cd services/ai-service-python
python -m pytest tests
```

Expected recently:

```text
23 passed
```

.NET:

```powershell
dotnet build services/backend-dotnet/TelegramAi.Backend/TelegramAi.Backend.csproj --no-restore
```

If backend is running, build may fail because `.dll`/`.exe` is locked. Use alternate output:

```powershell
dotnet build services/backend-dotnet/TelegramAi.Backend/TelegramAi.Backend.csproj --no-restore -o tmp/backend-build-check
```

## Recent commit context

Latest local commit at handoff creation time:

```text
c1f7232 Add resilient Mistral RAG ingestion flow
```

Important note: If this commit is not visible on GitHub, push may still be pending. The previous push attempt required explicit approval for:

```text
https://github.com/melihesensio99/personal-rag-agent.git
```

## Known issues / caveats

- Telegram conversation history is not portable across Codex accounts.
- API keys are local `.env`; never commit them.
- Current chunking is fixed-size character chunking, not semantic chunking.
- No embeddings yet.
- No pgvector yet.
- Search is not true semantic RAG yet; it is DB/filter based.
- Answer LLM is not implemented yet; search results are formatted by backend.
- PDF/image upload support is planned but not implemented.
- YouTube transcript availability depends on YouTube captions/transcript API behavior.
- Some transcripts can be in Hindi/English/Turkish; future improvement may store original + Turkish normalized text.

## Next recommended step

The next major engineering step should be:

```text
Embedding + pgvector for content_chunks
```

Recommended implementation order:

1. Add pgvector extension migration.
2. Add `Embedding` column to `content_chunks`.
3. Add Python embedding endpoint.
4. Use Mistral embedding model or another free embedding provider.
5. After chunk creation, generate embedding for each chunk.
6. Implement semantic search endpoint/query in backend.
7. For user search questions, retrieve top relevant chunks.
8. Add Answer LLM to generate natural Turkish response with source links.

## Prompt for another Codex account

When opening this repo in another Codex account, paste:

```text
Bu projeye devam etmek istiyorum. Önce docs/CODEX_HANDOFF.md dosyasını tamamen oku, sonra docs/ROADMAP.md ve docs/MANUAL_TEST_FLOW.md dosyalarını incele. Sohbet geçmişim yok; bu dosyayı proje hafızası olarak kabul et. Bana mevcut durumu kısa özetle ve sıradaki adım olan embedding + pgvector için plan çıkar. Kod yazmadan önce hangi dosyalara bakacağını söyle.
```

