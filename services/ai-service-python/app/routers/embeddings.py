from fastapi import APIRouter, Depends

from app.contracts.embeddings import EmbeddingRequest, EmbeddingResponse
from app.dependencies import get_embedding_service
from app.services.embedding_service import EmbeddingService

router = APIRouter(tags=["embeddings"])


@router.post("/api/v1/embeddings", response_model=EmbeddingResponse)
async def create_embeddings(
    request: EmbeddingRequest,
    embedding_service: EmbeddingService = Depends(get_embedding_service),
) -> EmbeddingResponse:
    return embedding_service.create_embeddings(request)
