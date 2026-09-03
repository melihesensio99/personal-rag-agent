from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class IntentRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    message: str = Field(min_length=1, max_length=2000)
    current_date: str = Field(min_length=10, max_length=10)


class IntentResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    action: Literal["save_content", "list_contents", "answer_from_memory", "ask_clarification"]
    intent: Literal["save", "search", "clarify"]
    query: str | None = Field(default=None, max_length=2000)
    content: str | None = Field(default=None, max_length=2000)
    content_kind: Literal["text", "video", "image"] | None = None
    source_type: Literal["article", "youtube", "pdf", "image", "telegram"] | None = None
    time_filter: Literal["today", "yesterday", "two_days_ago", "none"] = "none"
    date_from: str | None = Field(default=None, max_length=10)
    date_to: str | None = Field(default=None, max_length=10)
    keywords: list[str] = Field(default_factory=list)
    semantic_query: str | None = Field(default=None, max_length=2000)
    needs_clarification: bool = False
    clarification_message: str | None = Field(default=None, max_length=500)
