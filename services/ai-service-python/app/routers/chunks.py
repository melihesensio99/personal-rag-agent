from fastapi import APIRouter, Depends

from app.contracts.chunks import ChunkRequest, ChunkResponse
from app.dependencies import get_chunking_service
from app.services.chunking_service import ChunkingService

router = APIRouter(tags=["chunks"])


@router.post("/api/v1/chunks", response_model=ChunkResponse)
async def create_chunks(
    request: ChunkRequest,
    chunking_service: ChunkingService = Depends(get_chunking_service),
) -> ChunkResponse:
    return chunking_service.create_chunks(request)
