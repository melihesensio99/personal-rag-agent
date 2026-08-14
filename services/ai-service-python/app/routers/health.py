from fastapi import APIRouter

from app.config import settings
from app.contracts.health import HealthResponse

router = APIRouter(tags=["system"])


@router.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse(
        service=settings.service_name,
        status="healthy",
        version=settings.service_version,
    )
