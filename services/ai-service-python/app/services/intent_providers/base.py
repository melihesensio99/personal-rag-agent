from abc import ABC, abstractmethod

from app.contracts.intents import IntentRequest, IntentResponse


class IntentProvider(ABC):
    @abstractmethod
    def classify(self, request: IntentRequest) -> IntentResponse:
        raise NotImplementedError
