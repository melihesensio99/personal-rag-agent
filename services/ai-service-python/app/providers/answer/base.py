from abc import ABC, abstractmethod

from app.contracts.answers import AnswerRequest, AnswerResponse


class AnswerProvider(ABC):
    @abstractmethod
    def create_answer(self, request: AnswerRequest) -> AnswerResponse:
        raise NotImplementedError
