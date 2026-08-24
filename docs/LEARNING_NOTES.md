# Learning Notes - Personal RAG Agent

Bu dosya projede konuşulan AI/backend kavramlarını, bu projedeki gerçek karşılıklarıyla özetler.

## LLM

LLM, büyük dil modeli demektir. OpenAI, Gemini, Mistral gibi servislerdeki modeller bu gruba girer.

Bu projede LLM şu işler için kullanılıyor:

- Kullanıcı mesajı kaydetme mi arama mı anlamak.
- İçerikten Türkçe özet çıkarmak.
- İleride arama sonuçlarından doğal cevap üretmek.

Kullanılan aktif provider:

```text
Mistral
```

## Intent

Intent, kullanıcının niyetidir.

Örnekler:

```text
"şu linki kaydet" → save
"bugün attığım videoları getir" → search
"şunu bul" ama ne olduğu belirsiz → clarify
```

Projede intent akışı:

```text
Telegram message
→ .NET
→ Python /api/v1/intents
→ Mistral
→ structured intent JSON
→ .NET aksiyonu seçer
```

## Structured output

Structured output, LLM'in serbest yazı yerine backend'in parse edebileceği JSON formatında cevap dönmesidir.

Örnek:

```json
{
  "intent": "search",
  "content_kind": "video",
  "source_type": null,
  "time_filter": "today",
  "keywords": ["spor"],
  "needs_clarification": false
}
```

Neden önemli?

Backend serbest metni güvenilir okuyamaz. JSON ise parse edilebilir, validate edilebilir ve enumlara maplenebilir.

Projede sorun yaşandı:

Mistral bazen JSON'un başına/sonuna açıklama koydu. Bu yüzden JSON ayıklama ve retry eklendi.

## Extraction

Extraction, linkten veya dosyadan gerçek içeriği çıkarma işlemidir.

Örnek:

```text
YouTube linki
→ video title
→ channel
→ transcript
```

```text
Article linki
→ HTML indir
→ reklam/gereksiz alanları temizle
→ makale metnini çıkar
```

Projede extraction Python tarafında yapılır.

Önemli dosyalar:

```text
services/ai-service-python/app/services/extractors/article_extractor.py
services/ai-service-python/app/services/extractors/youtube_extractor.py
services/ai-service-python/app/services/extractors/youtube_transcript_provider.py
```

## Summary

Summary, içeriğin özetidir.

Projede summary şu alanlardan oluşur:

```json
{
  "title": "...",
  "short_summary": "...",
  "key_points": ["..."],
  "tags": ["..."],
  "language": "tr",
  "provider": "mistral"
}
```

Önemli ayrım:

- Summary, kullanıcıya gösterilecek kısa bilgi.
- Chunk, ileride RAG'ın arayacağı gerçek metin parçaları.

## Tags

Tags, içeriği temsil eden kısa konu etiketleridir.

Örnek:

```json
["rag", "embedding", "vector search", "backend"]
```

Projede tags LLM tarafından üretilir. Şu an arama/filter için kullanılabilir, ileride metadata olarak daha faydalı hale gelebilir.

## Chunk

Chunk, uzun metnin küçük parçalara bölünmüş halidir.

Neden var?

LLM'e her zaman tüm metni göndermek pahalı ve bazen gereksizdir. RAG sırasında sadece ilgili chunk'lar modele gönderilir.

Projede chunk örneği:

```text
content_chunks
```

Her chunk:

- content item id
- index
- text
- char start
- char end

Şu an chunking fixed-size karakter bazlıdır.

## Overlap

Overlap, iki chunk arasında küçük bir tekrar alanıdır.

Örnek:

```text
Text: ABCDEFGHIJ
chunk_size: 4
overlap: 1
```

Sonuç:

```text
ABCD
DEFG
GHIJ
```

Neden?

Chunk sınırında anlam kopmasını azaltır.

## Embedding

Embedding, metnin anlamını temsil eden sayı vektörüdür.

Örnek:

```text
"kas gelişimi için protein"
→ [0.012, -0.44, 0.88, ...]
```

Benzer anlamdaki metinler vektör uzayında birbirine yakın olur.

Bu projede embedding henüz yok. Sıradaki büyük adım budur.

## Vector database / pgvector

Vector database, embedding vektörlerini saklayıp benzerlik araması yapan veri tabanı türüdür.

Bu projede PostgreSQL kullanıldığı için plan:

```text
PostgreSQL + pgvector
```

Yani ayrı bir vector DB yerine PostgreSQL içinde vector column kullanacağız.

## RAG

RAG, Retrieval-Augmented Generation demektir.

Basit akış:

```text
1. İçerikleri çıkar
2. Chunk'lara böl
3. Chunk embedding üret
4. Kullanıcı soru sorunca soruyu embedding'e çevir
5. En alakalı chunk'ları getir
6. Bu chunk'ları LLM'e context olarak ver
7. LLM cevap üretsin
```

Şu an projedeki durum:

```text
Extract → Summary → Chunk → Store
```

Henüz yok:

```text
Embed → Vector Search → Answer LLM
```

## Naive RAG

İlk kuracağımız RAG türü.

Akış:

```text
Chunk
→ Embed
→ Store
→ Retrieve top K
→ Prompt
→ Answer
```

Şu an hedefimiz advanced/agentic RAG değil, önce anlaşılır çalışan Naive RAG.

## Hybrid search

Hybrid search, keyword search + vector search birleşimidir.

Örnek:

```text
BM25 / full-text search
+
semantic vector search
```

İleride faydalı olur ama ilk adım değildir.

## Re-ranking

Re-ranking, retrieval ile bulunan aday sonuçları tekrar sıralama işlemidir.

Örnek:

```text
Vector search top 20 getirir.
Re-ranker en iyi 5'i seçer.
```

Bu production kalitesi için önemlidir ama şu an erken aşamadır.

## Agent

Agent, LLM'in araç kullanarak adım adım karar verdiği yapıdır.

Örnek:

```text
Kullanıcı: "geçen hafta sporla ilgili attığım videoyu bul ve özetle"
Agent:
1. Intent çıkarır
2. Search tool çağırır
3. Sonuçları okur
4. Gerekirse başka arama yapar
5. Cevap üretir
```

Bu projede henüz full agent yok. Ama ileride Answer LLM + tool calling ile agentic RAG'e evrilebilir.

## Backend perspective

Bu projede AI öğrenirken backend tarafında da şu kavramlar çalışılıyor:

- Clean-ish architecture
- Application service
- Domain model
- Repository
- EF Core
- PostgreSQL
- API contract
- External service client
- Timeout
- Retry
- Structured validation
- Telegram bot polling

## Sıradaki öğrenme odağı

Sıradaki kavramlar:

1. Embedding nedir?
2. pgvector nasıl kurulur?
3. Chunk embedding DB'ye nasıl yazılır?
4. Kullanıcı sorusu embedding'e nasıl çevrilir?
5. Similarity search nasıl yapılır?
6. LLM'e retrieved context nasıl verilir?

