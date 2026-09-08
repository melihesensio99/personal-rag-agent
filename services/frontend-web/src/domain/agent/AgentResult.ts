export type AgentResultType = "content-list" | "answer" | "clarification";

export interface AgentSource {
  id: string;
  title: string;
  sourceType: "youtube" | "article";
  summary: string;
  durationLabel: string;
}

export interface AgentResult {
  type: AgentResultType;
  message: string;
  activityLabel: string;
  sources: AgentSource[];
  suggestions: string[];
}
