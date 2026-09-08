import type { AgentRepository } from "../../../domain/agent/AgentRepository";
import type { AgentResult } from "../../../domain/agent/AgentResult";

export class SendAgentMessageUseCase {
  public constructor(private readonly repository: AgentRepository) {}

  public execute(message: string): Promise<AgentResult> {
    const normalized = message.trim();
    if (!normalized) throw new Error("Bir mesaj yazmalısın.");
    return this.repository.send(normalized);
  }
}
