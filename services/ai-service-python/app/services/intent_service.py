from app.contracts.intents import IntentRequest, IntentResponse
from app.providers.intent.base import IntentProvider


class IntentService:
    def __init__(self, provider: IntentProvider) -> None:
        self._provider = provider

    def classify(self, request: IntentRequest) -> IntentResponse:
        return self._provider.classify(request)
