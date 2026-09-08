import { useState, type FormEvent } from "react";
import { ArrowUp, CheckCircle2, Sparkles } from "lucide-react";
import { sendAgentMessageUseCase } from "../app/dependencies";
import type { AgentResult } from "../domain/agent/AgentResult";
import { AgentSourceCard } from "../features/assistant/presentation/AgentSourceCard";

const starterPrompts = ["RAG kaynaklarım ne anlatıyor?", "Kaynaklardaki ortak fikirleri özetle", "Hibrit aramanın avantajı ne?"];

export function AssistantPage() {
  const [input, setInput] = useState("");
  const [question, setQuestion] = useState<string>();
  const [result, setResult] = useState<AgentResult>();
  const [isLoading, setIsLoading] = useState(false);

  async function send(message: string) {
    setQuestion(message); setResult(undefined); setIsLoading(true); setInput("");
    try { setResult(await sendAgentMessageUseCase.execute(message)); }
    finally { setIsLoading(false); }
  }
  function submit(event: FormEvent) { event.preventDefault(); void send(input); }

  return <div className="assistant-page">
    <header className="assistant-header"><div><p className="eyebrow">KAYNAK DESTEKLİ SOHBET</p><h1>AI Asistan</h1><p>Kayıtlı tüm kaynaklarına dayanarak soru sor ve kaynaklı yanıt al.</p></div><span className="online-badge"><span/> Kaynaklar hazır</span></header>
    <section className="conversation">
      {!question && <div className="assistant-welcome"><div className="assistant-mark"><Sparkles size={23}/></div><h2>Kaynaklarına ne sormak istersin?</h2><p>Cevaplar yalnızca kaydettiğin video ve makalelerdeki bilgilere dayanır.</p><div className="prompt-chips">{starterPrompts.map((prompt) => <button key={prompt} onClick={() => void send(prompt)}>{prompt}</button>)}</div></div>}
      {question && <div className="message user-message">{question}</div>}
      {isLoading && <div className="activity-row"><span className="loading-dot"/> Kaynaklar aranıyor…</div>}
      {result && <div className="assistant-response"><div className="activity-row complete"><CheckCircle2 size={16}/>{result.activityLabel}</div><div className="message assistant-message">{result.message}</div>{result.sources.length > 0 && <div className="agent-results">{result.sources.map((source) => <AgentSourceCard key={source.id} source={source}/>)}</div>}{result.suggestions.length > 0 && <div className="prompt-chips">{result.suggestions.map((item) => <button key={item} onClick={() => void send(item)}>{item}</button>)}</div>}</div>}
    </section>
    <form className="assistant-composer" onSubmit={submit}><input aria-label="AI Asistan mesajı" placeholder="Kaynakların hakkında bir soru sor…" value={input} onChange={(event) => setInput(event.target.value)}/><button aria-label="Gönder" disabled={isLoading || !input.trim()}><ArrowUp size={19}/></button></form>
    <p className="demo-note">Yanıtlar kayıtlı kaynaklarındaki bilgiler kullanılarak oluşturulur.</p>
  </div>;
}
