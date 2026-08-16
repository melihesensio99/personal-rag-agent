from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


class IntentService:
    def __init__(self, provider: IntentProvider) -> None:
        self._provider = provider

    def classify(self, request: IntentRequest) -> IntentResponse:
        return self._provider.classify(request)
