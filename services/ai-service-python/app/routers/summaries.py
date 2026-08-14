from fastapi import APIRouter, Depends

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.dependencies import get_summary_service
from app.services.summary_service import SummaryService

router = APIRouter(tags=["summaries"])


@router.post("/api/v1/summaries", response_model=SummaryResponse)
async def create_summary(
    request: SummaryRequest,
    summary_service: SummaryService = Depends(get_summary_service),
) -> SummaryResponse:
    return summary_service.create_summary(request)
