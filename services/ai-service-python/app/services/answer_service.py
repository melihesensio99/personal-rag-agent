from app.contracts.answers import AnswerRequest, AnswerResponse
from app.providers.answer.base import AnswerProvider


class AnswerService:
    def __init__(self, provider: AnswerProvider) -> None:
        self._provider = provider

    def create_answer(self, request: AnswerRequest) -> AnswerResponse:
        return self._provider.create_answer(request)
