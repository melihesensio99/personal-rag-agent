from fastapi import APIRouter, Depends

from app.contracts.intents import IntentRequest, IntentResponse
from app.dependencies import get_intent_service
from app.services.intent_service import IntentService

router = APIRouter(tags=["intents"])


@router.post("/api/v1/intents", response_model=IntentResponse)
async def classify_intent(
    request: IntentRequest,
    intent_service: IntentService = Depends(get_intent_service),
) -> IntentResponse:
    return intent_service.classify(request)
