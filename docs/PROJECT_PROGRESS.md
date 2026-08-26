# Personal RAG Agent - Project Progress

Bu dosya projede neleri bitirdigimizi, her adimin hangi konuya denk geldigini ve siradaki asamalari takip etmek icin yasayan checklist olarak tutulur.

## Legend

- `[x]` Bitti ve denendi
- `[~]` Calisiyor ama iyilestirme lazim
- `[ ]` Henuz yapilmadi

## 1. Proje Temeli ve Backend Iskeleti

- [x] Proje fikri netlestirildi: Telegram uzerinden icerik kaydedip RAG ile soru cevaplayan kisisel asistan.
  - Konu: product thinking, personal knowledge base, RAG use case
- [x] .NET backend ana servis olarak secildi.
  - Konu: backend ownership, API orchestration
- [x] Python FastAPI servisi AI/RAG yetenekleri icin ayrildi.
  - Konu: polyglot architecture, AI microservice
- [x] .NET ile Python arasinda HTTP/JSON contract kuruldu.
  - Konu: service-to-service communication, typed client, DTO
- [x] Clean-ish architecture klasorleri olusturuldu.
  - Konu: Application, Domain, Infrastructure, Api katmanlari

## 2. Telegram Giris Kanali

- [x] Telegram bot olusturuldu ve backend polling ile mesaj almaya basladi.
  - Konu: Telegram Bot API, polling, inbound channel
- [x] `/start` komutu icin karsilama mesaji eklendi.
  - Konu: bot UX
- [x] Telegram'dan gelen metin/link backend'e aktariliyor.
  - Konu: message handling, application service
- [~] Telegram'da soru/listeme/kaydetme ayrimi calisiyor.
  - Konu: intent routing
  - Not: Su an calisiyor ama Agent Router v1 ile daha temiz hale getirilecek.

## 3. Persistence ve Veri Modeli

- [x] PostgreSQL eklendi.
  - Konu: relational database
- [x] EF Core ile `contents` tablosu kuruldu.
  - Konu: ORM, migration, repository
- [x] `ContentItem`, `ContentSummary`, `ContentChunk` modelleri olustu.
  - Konu: domain model
- [x] `content_chunks` tablosu eklendi.
  - Konu: RAG storage, chunk persistence
- [x] pgvector extension ve `embedding vector(1024)` kolonu eklendi.
  - Konu: vector database, pgvector

## 4. Extraction

- [x] Text/Telegram metinlerini kaydetme akisi kuruldu.
  - Konu: raw text ingestion
- [x] Article URL extraction eklendi.
  - Konu: web scraping, HTML parsing, article extraction
- [x] Google search URL'leri direkt kaynak kabul edilmesin diye engellendi.
  - Konu: input validation
- [x] YouTube metadata extraction eklendi.
  - Konu: URL extraction, video metadata
- [x] YouTube transcript extraction eklendi.
  - Konu: transcript retrieval
- [~] Arxiv/PDF benzeri akademik kaynaklar link olarak islenebiliyor.
  - Konu: academic content ingestion
  - Not: Direkt PDF upload henuz yok.

## 5. Summary

- [x] Fake summary provider ile contract sabitlendi.
  - Konu: provider abstraction, contract-first development
- [x] Gemini denendi.
  - Konu: LLM provider integration
- [x] Mistral aktif summary provider olarak baglandi.
  - Konu: LLM API, structured JSON output
- [x] Ozetler Turkce donecek sekilde ayarlandi.
  - Konu: prompt behavior, localization
- [x] Summary alanlari olustu: title, short_summary, key_points, tags, language, provider.
  - Konu: structured output

## 6. Chunking

- [x] Extract edilen metin chunk'lara bolunuyor.
  - Konu: chunking
- [x] Chunk'lar index, text, charStart, charEnd ile DB'ye yaziliyor.
  - Konu: traceability
- [x] Overlap mantigi eklendi.
  - Konu: context preservation
- [~] Fixed-size chunking calisiyor.
  - Konu: naive chunking
  - Not: Daha sonra paragraph/sentence-aware chunking'e gecilecek.

## 7. Embedding ve Semantic Search

- [x] Fake embedding provider ile akis stabilize edildi.
  - Konu: test double, local development
- [x] Mistral embedding provider eklendi.
  - Konu: embedding model
- [x] Chunk embedding'leri DB'ye yaziliyor.
  - Konu: vector persistence
- [x] Kullanici sorusu embedding'e cevriliyor.
  - Konu: query embedding
- [x] pgvector ile cosine distance uzerinden semantic search yapiliyor.
  - Konu: vector similarity search
- [x] `POST /api/v1/search/semantic` endpoint'i eklendi.
  - Konu: retrieval API

## 8. Semantic Answer

- [x] Semantic search sonuclarini LLM'e verip cevap uretme eklendi.
  - Konu: RAG answer generation
- [x] `POST /api/v1/answers/semantic` endpoint'i eklendi.
  - Konu: answer API
- [x] Cevaplarda kaynak chunk'lar donuyor.
  - Konu: citations, grounded answer
- [x] `contentId` ile tek bir kayda odakli cevaplama eklendi.
  - Konu: scoped RAG
- [x] Farkli kaynaklar celisirse cevaba bunu yansitma prompt'u eklendi.
  - Konu: conflict-aware answer
- [x] Telegram'da soru sorunca semantic answer akisi baglandi.
  - Konu: Telegram RAG UX
- [x] Telegram kaynaklari ayni URL/content altinda gruplanacak sekilde duzenlendi.
  - Konu: response formatting

## 9. Intent ve Routing

- [x] LLM intent classifier eklendi.
  - Konu: intent classification
- [x] `save`, `search`, `clarify` intentleri calisiyor.
  - Konu: routing
- [x] Uzun icerik/metin `save` olarak korunuyor.
  - Konu: guardrail
- [x] Soru cumlesi yanlislikla `save` veya `clarify` olursa `search`e cekiliyor.
  - Konu: normalization, guardrail
- [~] Backend hala `search` geldikten sonra liste mi cevap mi kararini kismen kendi veriyor.
  - Konu: temporary orchestration
  - Not: Bu kisim Agent Router v1 ile kaldirilacak.

## 10. Postman ve Manuel Test

- [x] Postman collection hazirlandi.
  - Konu: API testing
- [x] Local environment hazirlandi.
  - Konu: developer experience
- [x] Content create, chunks, semantic search, semantic answer testleri eklendi.
  - Konu: manual verification
- [x] Telegram uzerinden real testler yapildi.
  - Konu: end-to-end test

## 11. GitHub ve Dokumantasyon

- [x] GitHub repo acildi: `personal-rag-agent`.
  - Konu: source control
- [x] Asamalar commitlere bolunerek pushlandi.
  - Konu: git hygiene
- [x] Architecture, roadmap, manual test flow ve learning notes dosyalari eklendi.
  - Konu: project documentation
- [~] Learning notes guncellenmeli.
  - Konu: documentation maintenance
  - Not: Embedding/RAG kisimlari eski bilgi iceriyor olabilir.

## Siradaki Buyuk Asama: Agent Router v1

- [ ] `IntentResponse` yerine `AgentDecisionResponse` tasarla.
  - Konu: LLM router, tool-based orchestration
- [ ] Action enumlarini netlestir.
  - Konu: structured decision
  - Planlanan actionlar: `save_content`, `list_contents`, `answer_from_memory`, `ask_clarification`
- [ ] Python router prompt'unu action sececek hale getir.
  - Konu: tool selection prompt
- [ ] .NET tarafinda `ResolveIntentAsync` yerine `ResolveAgentDecisionAsync` kur.
  - Konu: backend orchestration
- [ ] Backend'deki `ShouldUseSemanticAnswer` gibi karar kurallarini kaldir.
  - Konu: removing duplicated decision logic
- [ ] Backend'i sadece action executor gibi calistir.
  - Konu: safe tool execution
- [ ] Action bazli Telegram testleri yap.
  - Konu: end-to-end validation

## Sonraki RAG Iyilestirmeleri

- [ ] Hybrid search ekle.
  - Konu: BM25/full-text + vector search
- [ ] Re-ranking ekle.
  - Konu: retrieval optimization
- [ ] Chunking'i paragraph/sentence-aware hale getir.
  - Konu: chunk quality
- [ ] Article extraction temizligini artir.
  - Konu: trafilatura/readability tuning
- [ ] YouTube transcript olmayan videolar icin fallback stratejisi belirle.
  - Konu: media fallback
- [ ] PDF upload destegi ekle.
  - Konu: file ingestion, PDF parsing
- [ ] Image upload/OCR destegi ekle.
  - Konu: multimodal ingestion, OCR
- [ ] Kayitli olmayan link + soru birlikte gelirse once ingest edip sonra cevaplama akisi tasarla.
  - Konu: multi-step agent workflow
- [ ] `buna gore`, `dunku makale`, `az onceki video` gibi referans cozumleme ekle.
  - Konu: reference resolution
- [ ] Birden fazla aday kaynak varsa secenek sunma akisi ekle.
  - Konu: clarification UX

## Ogrenme Sirasi

- [x] LLM nedir?
- [x] Prompt ve structured output nedir?
- [x] Intent classification nedir?
- [x] Extraction nedir?
- [x] Summary nedir?
- [x] Chunk nedir?
- [x] Overlap nedir?
- [x] Embedding nedir?
- [x] pgvector nedir?
- [x] Semantic search nedir?
- [x] RAG nedir?
- [x] Citation/source grounding nedir?
- [~] Agent nedir?
  - Not: Agent Router v1 ile pratik olarak oturacak.
- [ ] Tool calling nedir?
- [ ] Hybrid search nedir?
- [ ] Re-ranking nedir?
- [ ] Evaluation/RAGAS nedir?

