import logging

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.summary_providers.base import SummaryProvider


logger = logging.getLogger(__name__)


class SummaryService:
    def __init__(
        self,
        primary_provider: SummaryProvider,
        fallback_provider: SummaryProvider | None = None,
    ) -> None:
        self._primary_provider = primary_provider
        self._fallback_provider = fallback_provider

    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        try:
            return self._primary_provider.create_summary(request)
        except Exception:
            if self._fallback_provider is None:
                raise

            logger.exception(
                "Primary summary provider failed for content_id=%s. Falling back to local summary provider.",
                request.content_id,
            )
            return self._fallback_provider.create_summary(request)
