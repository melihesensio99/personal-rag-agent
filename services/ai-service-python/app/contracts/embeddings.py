from pydantic import BaseModel, ConfigDict, Field, model_validator


class EmbeddingRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str = Field(min_length=1, max_length=200)
    texts: list[str] = Field(min_length=1, max_length=128)

    @model_validator(mode="after")
    def validate_texts(self) -> "EmbeddingRequest":
        for text in self.texts:
            if not text or not text.strip():
                raise ValueError("texts cannot include empty items")

            if len(text) > 10000:
                raise ValueError("each text item must be at most 10000 characters")

        return self


class TextEmbedding(BaseModel):
    model_config = ConfigDict(extra="forbid")

    index: int
    embedding: list[float]


class EmbeddingResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    content_id: str
    model: str
    dimension: int
    embeddings: list[TextEmbedding]
