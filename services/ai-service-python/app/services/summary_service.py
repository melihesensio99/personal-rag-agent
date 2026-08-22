from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.summary_providers.base import SummaryProvider


class SummaryService:
    def __init__(self, primary_provider: SummaryProvider) -> None:
        self._primary_provider = primary_provider

    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        return self._primary_provider.create_summary(request)
