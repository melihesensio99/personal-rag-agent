import { useState } from "react";
import { createSummaryUseCase } from "../app/dependencies";
import type { Content } from "../domain/content/Content";
import { SummaryForm } from "../features/quick-summary/presentation/SummaryForm";
import { SummaryResult } from "../features/quick-summary/presentation/SummaryResult";

export function QuickSummaryPage() {
  const [result, setResult] = useState<Content>();
  return (
    <div className="page">
      <header className="page-header"><p className="eyebrow">ARAŞTIRMA ALANI</p><h1>Hızlı Özet</h1><p>Bir video veya makale bağlantısını bilgiye dönüştür.</p></header>
      <div className="summary-grid">
        <SummaryForm title="YouTube videosunu özetle" description="Video bağlantısını yapıştır; transkripti çıkarıp özetleyelim." source="youtube" useCase={createSummaryUseCase} onCompleted={setResult}/>
        <SummaryForm title="Makale veya web linkini özetle" description="Bir web içeriğini temizleyip önemli noktaları çıkaralım." source="article" useCase={createSummaryUseCase} onCompleted={setResult}/>
      </div>
      {result && <SummaryResult content={result} />}
    </div>
  );
}
