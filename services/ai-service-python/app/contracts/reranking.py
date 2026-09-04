from pydantic import BaseModel, ConfigDict, Field


class RerankDocument(BaseModel):
    model_config = ConfigDict(extra="forbid")

    index: int
    text: str = Field(min_length=1, max_length=12000)


class RerankRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    query: str = Field(min_length=1, max_length=4000)
    documents: list[RerankDocument] = Field(min_length=1, max_length=64)


class RerankScore(BaseModel):
    index: int
    score: float


class RerankResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    model: str
    scores: list[RerankScore]
