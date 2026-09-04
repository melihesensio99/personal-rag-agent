from fastapi import APIRouter, Depends

from app.contracts.reranking import RerankRequest, RerankResponse
from app.dependencies import get_reranking_service
from app.services.reranking_service import RerankingService

router = APIRouter(tags=["reranking"])


@router.post("/api/v1/rerank", response_model=RerankResponse)
async def rerank(
    request: RerankRequest,
    reranking_service: RerankingService = Depends(get_reranking_service),
) -> RerankResponse:
    return reranking_service.rerank(request)
