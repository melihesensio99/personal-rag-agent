import { CheckCircle2, MessageSquareText } from "lucide-react";
import type { Content } from "../../../domain/content/Content";

export function SummaryResult({ content }: { content: Content }) {
  return (
    <section className="result-card">
      <div className="result-heading"><div><p className="eyebrow">SON ÖZETLENEN İÇERİK</p><h2>{content.title}</h2></div><span className="success-badge"><CheckCircle2 size={15}/> Kaydedildi</span></div>
      <p className="summary-copy">{content.summary}</p>
      {content.keyPoints.length > 0 && <div className="key-points">{content.keyPoints.slice(0, 4).map((point, index) => <div key={point}><span>{index + 1}</span><p>{point}</p></div>)}</div>}
      <button className="secondary-button"><MessageSquareText size={17}/> Bu kaynak hakkında sor</button>
    </section>
  );
}
