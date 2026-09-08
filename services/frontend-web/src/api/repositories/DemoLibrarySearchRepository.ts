import type { LibrarySearchRepository, LibrarySearchResult } from "../../domain/library/LibrarySearchRepository";
import type { LibrarySource, LibrarySourceType } from "../../domain/library/LibrarySource";
const demoSources: LibrarySource[] = [
  { id: "video-1", title: "Büyük Dil Modellerinin İnşası", sourceType: "youtube", durationLabel: "24 dk", createdAtLabel: "Bugün, 14:32", summary: "Tokenizasyon, ön eğitim ve RLHF süreçlerinin sade teknik anlatımı." },
  { id: "article-1", title: "Modern RAG Mimarilerinde Hibrit Arama", sourceType: "article", durationLabel: "8 dk okuma", createdAtLabel: "Bugün, 10:18", summary: "BM25, vektör araması ve cross-encoder reranking stratejileri." },
  { id: "video-2", title: "LangChain ve LlamaIndex Karşılaştırması", sourceType: "youtube", durationLabel: "42 dk", createdAtLabel: "Dün, 18:05", summary: "Ajan akışları ve veri indeksleme yaklaşımlarının mimari farkları." },
  { id: "article-2", title: "PostgreSQL pgvector Kurulum Rehberi", sourceType: "article", durationLabel: "11 dk okuma", createdAtLabel: "2 gün önce", summary: "HNSW indeksleme ve kosinüs benzerliği için uygulamalı rehber." },
];
export class DemoLibrarySearchRepository implements LibrarySearchRepository {
  public async getAll(type?: LibrarySourceType) { return type ? demoSources.filter((source) => source.sourceType === type) : demoSources; }
  public async search(command: string): Promise<LibrarySearchResult> {
    await new Promise((resolve) => setTimeout(resolve, 550));
    const normalized = command.toLocaleLowerCase("tr");
    const appliedType = normalized.includes("video") ? "youtube" : normalized.includes("makale") ? "article" : undefined;
    let sources = await this.getAll(appliedType);
    if (normalized.includes("2 gün") || normalized.includes("iki gün")) sources = sources.filter((source) => source.createdAtLabel === "2 gün önce");
    return { message: `${sources.length} kaynak bulundu`, sources, appliedType };
  }
}
