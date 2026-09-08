import type { AgentResult } from "./AgentResult";

export interface AgentRepository {
  send(message: string): Promise<AgentResult>;
}
