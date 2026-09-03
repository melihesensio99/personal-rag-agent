# Personal RAG Agent Roadmap

Bu dosya, proje ilerlerken unutulmaması gereken teknik borçları ve sonraki geliştirme adımlarını tutar.

## Şu an çalışan ana parçalar

- Telegram bot üzerinden metin/link alma.
- Python AI service ile intent sınıflandırma.
- Mistral ile intent, summary ve answer üretme.
- Article URL extraction ile makale metni çekme.
- YouTube metadata/transcript extraction denemesi.
- İçeriği PostgreSQL'e `contents` olarak kaydetme.
- Extract edilen metni chunk'lara bölme.
- Chunk'ları PostgreSQL `content_chunks` tablosuna düz metin olarak kaydetme.
- Chunk'lar için Mistral/fake embedding üretme.
- Embedding'leri PostgreSQL `vector(1024)` kolonunda saklama.
- `POST /api/v1/search/semantic` ile kullanıcı sorgusuna en yakın chunk'ları pgvector üzerinden bulma.
- `POST /api/v1/search/answer` ile bulunan chunk'lar üzerinden Türkçe cevap üretme.
- `contentId` filtresi ile tek bir kayıt/link üzerinden semantic search ve answer yapabilme.
- Postman üzerinden content, chunk, semantic search ve semantic answer debug kayıtlarını inceleme.

## Kısa vadeli iyileştirme backlog'u

### 1. Agent decision contract v2

Amaç: LLM'in serbest string yerine numeric code döndürmesi.

Planlanan yapı:

```json
{
  "intent_code": 1,
  "content_kind_code": 2,
  "source_type_code": 0,
  "date_from": "2026-08-20",
  "date_to": "2026-08-21",
  "keywords": ["spor"],
  "needs_clarification": false,
  "clarification_question": null
}
```

Kodlar:

- `intent_code`: `0 = save`, `1 = search`, `2 = clarify`
- `content_kind_code`: `0 = unknown`, `1 = text`, `2 = video`, `3 = image`
- `source_type_code`: `0 = unknown`, `1 = article`, `2 = youtube`, `3 = pdf`, `4 = image`, `5 = telegram`

Neden: Mistral/Gemini bazen `search` yerine `retrieve`, `list`, `find` gibi değerler döndürebiliyor. Numeric contract hata ihtimalini azaltır.

### 2. Akıllı tarih aralığı

Amaç: `today/yesterday` gibi sabit enum yerine LLM'in doğrudan tarih aralığı üretmesi.

Örnekler:

- “bugün attığım videolar” → `date_from=2026-08-21`, `date_to=2026-08-22`
- “geçen hafta attığım spor linkleri” → gerçek haftalık tarih aralığı
- “15 Ağustos'ta attığım pdfler” → `2026-08-15` / `2026-08-16`
- “tarih ile ilgili videolar” → `keywords=["tarih"]`, tarih filtresi yok

Neden: Kullanıcı doğal konuşur; zamanı hardcoded kurallarla değil LLM ile structured date range'e çevirmek gerekir.

### 3. Article extraction kalitesini artırma

Amaç: Makale metni çıkarılırken başlık, paragraf ve liste yapısını daha iyi korumak.

Şu an:

- Metin çekiliyor.
- Chunk'lara bölünüyor.
- Fakat bazı heading/list yapıları düz metne dönüşebiliyor.

İyileştirme:

- Trafilatura markdown output'u daha etkin kullan.
- Gereksiz image/ad text alanlarını temizle.
- Başlıkları ve bullet listeleri RAG için koru.

### 4. Chunk kalitesini artırma

Amaç: Sabit karakter bazlı chunk yerine daha anlamlı chunk üretmek.

Şu an:

- Fixed-size chunking.
- Overlap var.

İyileştirme:

- Paragraf/sentence-aware chunking.
- Markdown heading'e göre chunking.
- Çok kısa/çok uzun chunk'ları normalize etme.

### 5. Embedding + pgvector (tamamlandı)

Amaç: Chunk'ları gerçek RAG aramasına hazır hale getirmek.

Durum:

- PostgreSQL'e pgvector extension eklendi.
- `content_chunks` tablosuna `embedding vector(1024)` kolonu eklendi.
- Mistral embedding modeli ile her chunk için embedding üretimi eklendi.
- Query geldiğinde kullanıcı sorusu embedding'e çevriliyor.
- pgvector cosine distance ile en alakalı chunk'lar bulunuyor.

Tamamlanan sonuç: Semantic search ve Answer LLM Telegram soru akışına bağlandı; cevap kaynak chunk'larıyla birlikte dönüyor.

### 6. Answer LLM (tamamlandı)

Amaç: Search sonucu bulunan kayıtları sadece listelemek yerine LLM ile doğal cevap üretmek.

Akış:

```text
Kullanıcı sorusu
→ Intent LLM
→ Backend retrieval
→ İlgili content/chunk sonuçları
→ Answer LLM
→ Türkçe doğal cevap + kaynak linkleri
```

Durum: Semantic search sonuçları Python Answer LLM'e gönderiliyor; Türkçe, kaynak-temelli cevap ve çelişen görüş karşılaştırması üretiliyor. `contentId` ile tek kayda odaklanma destekleniyor.

### 7. Clarify flow

Amaç: LLM mesajı net anlayamazsa yanlış işlem yapmak yerine kullanıcıdan açıklama istemek.

Örnek:

```text
Kullanıcı: "şunu getir"
Bot: "Neyi getirmemi istiyorsun? Konu, tarih veya içerik türü söyleyebilir misin?"
```

### 8. Provider abstraction temizliği

Amaç: Gemini/Mistral/OpenRouter gibi provider'ları kolay değiştirilebilir hale getirmek.

Notlar:

- Aktif provider şu an Mistral.
- Gemini provider kodda durabilir ama aktif değil.
- Provider çıktıları doğrudan backend'e güvenilerek aktarılmamalı; validate/normalize edilmeli.

### 9. Belirli kaynak bağlamında soru-cevap

Amaç: Aynı konuya ait birçok makale veya video varken kullanıcının yalnızca seçtiği kaynağa göre cevap alabilmesi.

Planlanan kullanıcı deneyimleri:

- Kullanıcı soruya link ekler: `Bu makaleye göre mikroservis ne zaman mantıklı? https://...`
- Kullanıcı kaydedilmiş kaynak mesajına Telegram reply ile soru sorar.
- Kullanıcı başlık veya URL seçerek kaynak bağlamını belirtir.

Teknik akış:

```text
Kaynak URL/kimliği çöz
→ contentId belirle
→ semantic search'ü contentId ile sınırla
→ yalnızca seçilen kaynağın chunk'larını Answer LLM'e gönder
```

Not: Backend API'si `contentId` filtresini şimdiden destekliyor; eksik olan Telegram tarafında kullanıcının doğal mesajından doğru kaynağı çözme deneyimidir. YouTube ve makaleler aynı akışı kullanır.

### 10. Dosya/görsel/PDF desteği

Amaç: Telegram'dan direkt PDF veya görsel yüklendiğinde de işleyebilmek.

Plan:

- Telegram file download.
- PDF text extraction.
- Image OCR.
- Sonra aynı pipeline: extraction → summary → chunk → embedding.

## Orta vadeli hedef

Naive RAG:

```text
Extract
→ Chunk
→ Embed
→ Store in pgvector
→ Retrieve top chunks
→ Answer with LLM
```

İlk hedefimiz production-level advanced RAG değil; önce çalışan ve anlaşılır Naive RAG kurmak.
