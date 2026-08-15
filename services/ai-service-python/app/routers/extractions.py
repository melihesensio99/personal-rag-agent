from fastapi import APIRouter, Depends

from app.contracts.extractions import ExtractionRequest, ExtractionResponse
from app.dependencies import get_extraction_service
from app.services.extraction_service import ExtractionService

router = APIRouter(tags=["extractions"])


@router.post("/api/v1/extractions", response_model=ExtractionResponse)
async def create_extraction(
    request: ExtractionRequest,
    extraction_service: ExtractionService = Depends(get_extraction_service),
) -> ExtractionResponse:
    return extraction_service.extract(request)
