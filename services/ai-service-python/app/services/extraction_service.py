from app.contracts.extractions import ExtractionRequest, ExtractionResponse
from app.services.extractors.article_extractor import ArticleExtractor
from app.services.extractors.youtube_extractor import YouTubeExtractor


class ExtractionService:
    def __init__(
        self,
        article_extractor: ArticleExtractor,
        youtube_extractor: YouTubeExtractor,
    ) -> None:
        self._article_extractor = article_extractor
        self._youtube_extractor = youtube_extractor

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        if request.source_type == "article" and request.url is not None:
            return self._article_extractor.extract(request)

        if request.source_type == "youtube" and request.url is not None:
            return self._youtube_extractor.extract(request)

        if request.text:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=request.source_type,
                extraction_status="unsupported",
                title=None,
                extracted_text=request.text.strip(),
                original_url=str(request.url) if request.url else None,
            )

        return ExtractionResponse(
            content_id=request.content_id,
            source_type=request.source_type,
            extraction_status="unsupported",
            title=None,
            extracted_text="",
            original_url=str(request.url) if request.url else None,
        )
