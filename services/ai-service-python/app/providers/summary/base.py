from abc import ABC, abstractmethod

from app.contracts.summaries import SummaryRequest, SummaryResponse


class SummaryProvider(ABC):
    @abstractmethod
    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        raise NotImplementedError
