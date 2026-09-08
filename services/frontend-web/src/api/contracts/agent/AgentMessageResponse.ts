export type AgentResponseType = "content_list" | "answer" | "clarification";

export interface AgentSourceResponse {
  id: string;
  title: string;
  sourceType: "YouTube" | "Article";
  summary: string;
  durationLabel: string;
}

export interface AgentMessageResponse {
  responseType: AgentResponseType;
  message: string;
  activityLabel: string;
  sources: AgentSourceResponse[];
  suggestions?: string[];
}
