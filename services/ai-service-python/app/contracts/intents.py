from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class IntentRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    message: str = Field(min_length=1, max_length=2000)
    current_date: str = Field(min_length=10, max_length=10)


class IntentResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    intent: Literal["save", "search", "clarify"]
    content_kind: Literal["text", "video", "image"] | None = None
    source_type: Literal["article", "youtube", "pdf", "image", "telegram"] | None = None
    time_filter: Literal["today", "yesterday", "two_days_ago", "none"] = "none"
    keywords: list[str] = Field(default_factory=list)
    needs_clarification: bool = False
