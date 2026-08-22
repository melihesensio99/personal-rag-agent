from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class SummaryRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    text: str = Field(min_length=1, max_length=20000)


class SummaryResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    title: str
    short_summary: str
    key_points: list[str]
    tags: list[str]
    language: str
    provider: Literal["fake", "gemini", "mistral"]
