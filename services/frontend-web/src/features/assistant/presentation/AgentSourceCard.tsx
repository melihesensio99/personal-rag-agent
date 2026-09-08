import { ArrowUpRight, BookOpen, CirclePlay, MessageSquareText } from "lucide-react";
import type { AgentSource } from "../../../domain/agent/AgentResult";

export function AgentSourceCard({ source }: { source: AgentSource }) {
  const Icon = source.sourceType === "youtube" ? CirclePlay : BookOpen;
  return <article className="agent-source-card">
    <div className="source-type"><Icon size={16}/>{source.sourceType === "youtube" ? "YouTube" : "Makale"}<span>{source.durationLabel}</span></div>
    <h3>{source.title}</h3><p>{source.summary}</p>
    <div className="card-actions"><button><ArrowUpRight size={15}/> Aç</button><button><MessageSquareText size={15}/> Bu kaynağa sor</button></div>
  </article>;
}
