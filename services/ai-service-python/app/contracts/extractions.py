from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, HttpUrl


class ExtractionRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    source_type: Literal["article", "youtube", "pdf", "image", "telegram"]
    url: HttpUrl | None = None
    text: str | None = Field(default=None, min_length=1, max_length=50000)


class ExtractionMetadata(BaseModel):
    model_config = ConfigDict(extra="forbid")

    domain: str | None = None
    content_type: str | None = None
    final_url: str | None = None
    extra: dict[str, Any] = Field(default_factory=dict)


class ExtractionResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    source_type: str
    extraction_status: Literal["completed", "unsupported", "failed"]
    title: str | None = None
    extracted_text: str = ""
    original_url: str | None = None
    metadata: ExtractionMetadata = Field(default_factory=ExtractionMetadata)
