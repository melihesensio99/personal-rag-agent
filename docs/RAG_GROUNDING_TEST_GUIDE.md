# RAG Kaynak Kullanımı ve Grounding Test Rehberi

## Amaç

Bu doküman, bir cevabın kayıtlı içeriklerden getirilen chunk'lara dayanıp dayanmadığını nasıl kontrol edeceğimizi açıklar.

`used_chunk_indexes` modelin beyanıdır; tek başına kesin kanıt değildir. Güvenilir kontrol için retrieval, reranking, LLM context'i ve cevap birlikte incelenmelidir.

## Mevcut akış

```text
Kullanıcı sorusu
  -> semantic retrieval
  -> reranking
  -> seçilen chunk'ların Context ID ile numaralandırılması
  -> answer LLM çağrısı
  -> used_chunk_indexes doğrulaması
  -> Telegram cevabı ve kaynak kartları
```

LLM'e gönderilen iki numara birbirinden ayrıdır:

```text
Context ID: 0              # Bu cevap isteğindeki sıra numarası
Original chunk number: 6   # İçeriğin veritabanındaki gerçek chunk numarası
```

Model yalnızca `Context ID` değerlerini `used_chunk_indexes` içinde döndürmelidir. Backend, geçersiz veya context dışı index'leri yok sayar. Citation boşsa Telegram'da tüm kaynaklar gösterilmez.

## Debug endpoint'i

Backend çalışırken şu endpoint kullanılabilir:

```http
POST http://localhost:8080/api/v1/search/answer/debug
Content-Type: application/json
```

Örnek istek:

```json
{
  "query": "RAG-Sequence ve RAG-Token arasındaki fark nedir?",
  "maxResults": 8
}
```

Yanıtta şu alanlar incelenmelidir:

| Alan | Anlamı |
|---|---|
| `rerankCandidates` | Aday chunk'ların skorları ve seçilip seçilmediği |
| `contextChunksSentToLlm` | LLM'e gerçekten gönderilen chunk'lar |
| `usedChunkIndexes` | Modelin kullandığını belirttiği Context ID değerleri |
| `answer` | Üretilen cevap |
| `sources` | Retrieval sonucu bulunan kaynaklar |

Örnek yorum:

```text
contextChunksSentToLlm[0].Index = 0
contextChunksSentToLlm[0].ChunkIndex = 6
usedChunkIndexes = [0]
```

Bu, modelin Context ID 0'ı kullandığını ve bunun orijinal chunk 6 olduğunu gösterir.

## Canary testi: en pratik doğrulama

Modelin önceden bilemeyeceği benzersiz bir test bilgisi kaydedilir:

```text
RAG doğrulama test kodu: MOR-KARTAL-742
```

Sonra şu soru sorulur:

```text
RAG doğrulama test kodu nedir?
```

Beklenen sonuçlar:

1. `rerankCandidates` içinde bu bilgiyi içeren chunk `Accepted: true` olmalı.
2. Aynı chunk `contextChunksSentToLlm` içinde bulunmalı.
3. `usedChunkIndexes`, ilgili Context ID'yi içermeli.
4. Cevap `MOR-KARTAL-742` değerini vermeli.

Bu test başarılıysa cevabın retrieval context'inden yararlandığına dair güçlü kanıt elde edilir.

## Ablation testi

Aynı soru iki farklı context ile çalıştırılır:

```text
Deney A: ilgili chunk dahil
Deney B: ilgili chunk çıkarılmış
```

Chunk çıkarıldığında cevap değişmeli veya sistem yeterli bilgi olmadığını belirtmelidir. Cevap hiç değişmiyorsa modelin parametric bilgisini kullanıyor olma ihtimali vardır.

## Örnek RAG makalesi incelemesi

RAG-Sequence açıklaması için `RAG-Sequence aynı dokümanı tüm dizi boyunca kullanır` iddiası Methods/Models bölümündeki ilgili chunk tarafından desteklenebilir. Ancak cevap içinde yazan `Chunk 0 ve 1` ifadesi, debug çıktısındaki gerçek context ve original chunk numaralarıyla eşleşmiyorsa geçerli citation kabul edilmemelidir.

Bir cevabın kaynak kanıtı şu şekilde değerlendirilir:

```text
Retrieval çalışmış mı?       -> rerankCandidates ve contextChunksSentToLlm
Context LLM'e gitmiş mi?     -> contextChunksSentToLlm
Model hangi context'i seçmiş?-> usedChunkIndexes
İddia chunk'ta var mı?       -> chunk TextPreview / gerçek chunk metni
```

## Çalıştırılan otomatik kontroller

Son değişikliklerden sonra aşağıdaki kontroller başarılıdır:

```text
python -m compileall -q app
python -m pytest -q tests/test_extractions.py tests/test_answers.py tests/test_chunks.py -p no:cacheprovider
```

Python servisinin tamamı çalıştırıldığında sonuç: 55 test geçti.

Summary güvenilirliği için ayrıca şu durumlar test edilir:

- Geçerli ve dış metinle çevrelenmiş JSON'un parse edilmesi
- Uzun summary girdisinin 40.000 karakterlik summary bütçesine sıkıştırılması
- Geçersiz alan şekli geldiğinde repair/retry akışı
- Mistral timeout veya bozuk JSON döndürdüğünde deterministik fallback summary
- Summary alanlarındaki `**`, `__` ve backtick işaretlerinin temizlenmesi

Summary fallback yalnızca metadata üretir. Kaynak içerik, chunk'lar, embedding'ler
ve RAG retrieval akışı bundan etkilenmez.

Ek olarak:

```text
dotnet build services/backend-dotnet/TelegramAi.Backend.Application/TelegramAi.Backend.Application.csproj --no-restore
```

Sonuç: 0 hata, 0 uyarı.

## Canlı canary testi

10 Eylül 2026 tarihinde yerel PostgreSQL, AI service ve .NET backend ile gerçek bir canlı test çalıştırıldı.

Test içeriğine yalnızca bu kayda ait olan şu değer eklendi:

```text
MOR-KARTAL-742
```

Soru:

```text
Bu kayda özel doğrulama kodu nedir?
```

Canlı sonuç:

```text
Cevap: Bu kayda özel doğrulama kodu 'MOR-KARTAL-742' dir.
UsedChunkIndexes: [0]
ContextChunksSentToLlm[0].ChunkIndex: 0
Similarity: 0.8373
RerankScore: 0.7296
Decision: selected_for_answer
Provider: mistral
```

Değerlendirme: Canary test başarılıdır. Benzersiz kod veritabanına kaydedilmiş, embedding oluşturulmuş, ilgili chunk retrieval ve reranking aşamalarından geçmiş, LLM context'ine gönderilmiş ve cevapta doğru değer üretilmiştir. Test kaydı daha sonra `DELETE /api/v1/contents/{id}` ile silinmiştir.

İlk canlı denemede backend'in `localhost:8000` adresi ile AI service'in `127.0.0.1:8000` binding'i arasında bağlantı problemi görüldü. Backend test sırasında `AiService__BaseUrl=http://127.0.0.1:8002` ile çalıştırıldı; bu instance yalnızca test için summary provider'ı fake, embedding/reranking/answer akışlarını gerçek provider olarak kullandı. Bu nedenle canlı canary sonucu retrieval/context/answer akışını doğrular. Normal Mistral summary çağrısında gözlenen bozuk JSON ve timeout problemleri için compact prompt, düşük output bütçesi ve deterministic fallback eklendi; provider'a ait canlı summary çağrısı da geçerli JSON ile sonuçlandı.

## Sınır

Bu kontroller retrieval'ın ve context aktarımının doğru olduğunu güçlü biçimde gösterir; fakat modelin her cümleyi yalnızca chunk'tan ürettiğini matematiksel olarak kanıtlamaz. Böyle bir garanti için ayrıca grounding validator gerekir. Maliyet nedeniyle şu an sistemde ikinci bir LLM doğrulama çağrısı kullanılmıyor.
