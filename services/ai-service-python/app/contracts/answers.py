from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class AnswerContextChunk(BaseModel):
    model_config = ConfigDict(extra="forbid")

    index: int = Field(ge=0)
    content_id: str = Field(min_length=1, max_length=200)
    chunk_id: str = Field(min_length=1, max_length=200)
    content_title: str = Field(min_length=1, max_length=500)
    content_url: str = Field(min_length=1, max_length=2000)
    source_type: str = Field(min_length=1, max_length=50)
    content_kind: str = Field(min_length=1, max_length=50)
    chunk_index: int = Field(ge=0)
    text: str = Field(min_length=1, max_length=25000)
    distance: float = Field(ge=0)
    similarity: float = Field(ge=0, le=1)


class AnswerRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    question: str = Field(min_length=1, max_length=2000)
    chunks: list[AnswerContextChunk] = Field(default_factory=list)


class AnswerResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    answer: str
    used_chunk_indexes: list[int]
    language: str
    provider: Literal["fake", "gemini", "mistral"]
