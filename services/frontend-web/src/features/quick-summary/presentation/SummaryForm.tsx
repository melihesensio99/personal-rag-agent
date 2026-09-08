import { useState, type FormEvent } from "react";
import { ArrowRight, Link2 } from "lucide-react";
import type { Content, ContentSource } from "../../../domain/content/Content";
import type { CreateSummaryUseCase } from "../application/CreateSummaryUseCase";

interface SummaryFormProps {
  title: string;
  description: string;
  source: ContentSource;
  useCase: CreateSummaryUseCase;
  onCompleted(content: Content): void;
}

export function SummaryForm({ title, description, source, useCase, onCompleted }: SummaryFormProps) {
  const [url, setUrl] = useState("");
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(undefined);
    setIsLoading(true);
    try { onCompleted(await useCase.execute(url, source)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Bir hata oluştu."); }
    finally { setIsLoading(false); }
  }

  return (
    <form className="summary-form" onSubmit={handleSubmit}>
      <div className={`source-icon ${source}`}><Link2 size={21} /></div>
      <div><p className="eyebrow">{source === "youtube" ? "YOUTUBE" : "WEB & BLOG"}</p><h2>{title}</h2><p>{description}</p></div>
      <label><span>Bağlantı</span><input value={url} onChange={(event) => setUrl(event.target.value)} placeholder="https://" /></label>
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="primary-button" disabled={isLoading}>{isLoading ? "Özet hazırlanıyor…" : "Özetle ve kaydet"}<ArrowRight size={17} /></button>
    </form>
  );
}
