from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, HttpUrl


class ExtractionRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    source_type: Literal["article", "youtube", "pdf", "image", "telegram", "instagram"] | None = None
    url: HttpUrl | None = None
    text: str | None = Field(default=None, min_length=1, max_length=50000)


class ExtractionMetadata(BaseModel):
    model_config = ConfigDict(extra="forbid")

    domain: str | None = None
    content_type: str | None = None
    final_url: str | None = None
    extra: dict[str, Any] = Field(default_factory=dict)


class ReaderBlock(BaseModel):
    model_config = ConfigDict(extra="forbid")

    type: Literal["heading", "paragraph", "image", "list", "quote"]
    text: str | None = None
    level: int | None = Field(default=None, ge=1, le=6)
    url: str | None = None
    caption: str | None = None
    items: list[str] | None = None


class ExtractionResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    source_type: str
    detected_content_kind: Literal["text", "video", "image", "unknown"] = "unknown"
    extraction_status: Literal["completed", "unsupported", "failed"]
    title: str | None = None
    extracted_text: str = ""
    original_url: str | None = None
    reader_blocks: list[ReaderBlock] = Field(default_factory=list)
    metadata: ExtractionMetadata = Field(default_factory=ExtractionMetadata)
