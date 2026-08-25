from fastapi import APIRouter, Depends

from app.contracts.answers import AnswerRequest, AnswerResponse
from app.dependencies import get_answer_service
from app.services.answer_service import AnswerService

router = APIRouter(tags=["answers"])


@router.post("/api/v1/answers", response_model=AnswerResponse)
async def create_answer(
    request: AnswerRequest,
    answer_service: AnswerService = Depends(get_answer_service),
) -> AnswerResponse:
    return answer_service.create_answer(request)
