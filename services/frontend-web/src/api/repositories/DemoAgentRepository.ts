import type { AgentMessageResponse, AgentSourceResponse } from "../contracts/agent/AgentMessageResponse";
import { mapAgentResponse } from "../mappers/agentMapper";
import type { AgentRepository } from "../../domain/agent/AgentRepository";
import type { AgentResult } from "../../domain/agent/AgentResult";

const sources: AgentSourceResponse[] = [
  { id: "video-1", title: "Büyük Dil Modellerinin İnşası", sourceType: "YouTube", durationLabel: "24 dk", summary: "Tokenizasyon, ön eğitim ve RLHF süreçlerinin sade teknik anlatımı." },
  { id: "video-2", title: "LangChain ve LlamaIndex Karşılaştırması", sourceType: "YouTube", durationLabel: "42 dk", summary: "Ajan akışları ve veri indeksleme yaklaşımlarının mimari farkları." },
  { id: "article-1", title: "Modern RAG Mimarilerinde Hibrit Arama", sourceType: "Article", durationLabel: "8 dk okuma", summary: "BM25, vektör araması ve cross-encoder reranking stratejileri." },
  { id: "article-2", title: "PostgreSQL pgvector Kurulum Rehberi", sourceType: "Article", durationLabel: "11 dk okuma", summary: "HNSW indeksleme ve kosinüs benzerliği için uygulamalı rehber." },
];

export class DemoAgentRepository implements AgentRepository {
  public async send(message: string): Promise<AgentResult> {
    await new Promise((resolve) => setTimeout(resolve, 850));
    const normalized = message.toLocaleLowerCase("tr");
    if (normalized.includes("rag") || normalized.includes("hibrit") || normalized.includes("arama")) {
      return mapAgentResponse({ responseType: "answer", activityLabel: "2 kaynak kullanıldı", message: "Kaynaklarında hibrit arama; BM25 anahtar kelime eşleşmesini vektör benzerliğiyle birleştiriyor. En iyi adaylar daha sonra cross-encoder ile yeniden sıralanıyor.", sources: sources.filter((source) => source.id.startsWith("article")) });
    }

    const response: AgentMessageResponse = { responseType: "answer", activityLabel: "3 kaynak kullanıldı", message: "Kayıtlı kaynaklarına göre büyük dil modelleri ön eğitimle genel dil yeteneği kazanıyor; RAG ise modele güncel ve sana ait bilgileri cevap anında sağlayarak daha güvenilir sonuç üretiyor.", sources: sources.slice(0, 3), suggestions: ["RAG hangi durumlarda tercih edilmeli?", "Kaynaklardaki ortak fikirleri özetle"] };
    return mapAgentResponse(response);
  }
}
