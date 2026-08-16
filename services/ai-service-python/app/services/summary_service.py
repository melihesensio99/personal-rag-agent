from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.summary_providers.base import SummaryProvider


class SummaryService:
    def __init__(self, provider: SummaryProvider) -> None:
        self._provider = provider

    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        return self._provider.create_summary(request)
