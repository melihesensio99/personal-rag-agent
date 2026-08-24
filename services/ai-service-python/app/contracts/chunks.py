from pydantic import BaseModel, ConfigDict, Field, model_validator


class ChunkRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    text: str = Field(min_length=1, max_length=250000)
    chunk_size: int = Field(default=1200, ge=2, le=4000)
    overlap: int = Field(default=150, ge=0, le=1000)

    @model_validator(mode="after")
    def validate_overlap(self) -> "ChunkRequest":
        if self.overlap >= self.chunk_size:
            raise ValueError("overlap must be smaller than chunk_size")

        return self


class TextChunk(BaseModel):
    model_config = ConfigDict(extra="forbid")

    index: int
    text: str
    char_start: int
    char_end: int


class ChunkResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    chunk_size: int
    overlap: int
    total_chunks: int
    chunks: list[TextChunk]
