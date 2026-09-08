import type { AgentMessageResponse } from "../contracts/agent/AgentMessageResponse";
import type { AgentResult } from "../../domain/agent/AgentResult";

export function mapAgentResponse(response: AgentMessageResponse): AgentResult {
  return {
    type: response.responseType.replace("_", "-") as AgentResult["type"],
    message: response.message,
    activityLabel: response.activityLabel,
    suggestions: response.suggestions ?? [],
    sources: response.sources.map((source) => ({
      ...source,
      sourceType: source.sourceType.toLowerCase() as "youtube" | "article",
    })),
  };
}
