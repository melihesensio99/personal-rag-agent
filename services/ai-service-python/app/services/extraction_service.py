from app.contracts.extractions import ExtractionRequest, ExtractionResponse
from app.services.extractors.article_extractor import ArticleExtractor
from app.services.extractors.youtube_extractor import YouTubeExtractor
from urllib.parse import urlparse


class ExtractionService:
    def __init__(
        self,
        article_extractor: ArticleExtractor,
        youtube_extractor: YouTubeExtractor,
    ) -> None:
        self._article_extractor = article_extractor
        self._youtube_extractor = youtube_extractor

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        detected_source_type = self._detect_source_type(request)
        normalized_request = request.model_copy(update={"source_type": detected_source_type})

        if detected_source_type == "article" and normalized_request.url is not None:
            return self._article_extractor.extract(normalized_request)

        if detected_source_type == "youtube" and normalized_request.url is not None:
            return self._youtube_extractor.extract(normalized_request)

        if normalized_request.text:
            return ExtractionResponse(
                content_id=normalized_request.content_id,
                source_type=detected_source_type,
                detected_content_kind=self._infer_content_kind(detected_source_type, normalized_request.url),
                extraction_status="unsupported",
                title=None,
                extracted_text=normalized_request.text.strip(),
                original_url=str(normalized_request.url) if normalized_request.url else None,
            )

        return ExtractionResponse(
            content_id=normalized_request.content_id,
            source_type=detected_source_type,
            detected_content_kind=self._infer_content_kind(detected_source_type, normalized_request.url),
            extraction_status="unsupported",
            title=None,
            extracted_text="",
            original_url=str(normalized_request.url) if normalized_request.url else None,
        )

    @staticmethod
    def _infer_content_kind(source_type: str, url: object | None) -> str:
        return {
            "youtube": "video",
            "image": "image",
            "article": "text",
            "pdf": "text",
            "telegram": "text",
            "instagram": "video" if url and ExtractionService._is_instagram_reel(str(url)) else "text",
        }.get(source_type, "unknown")

    @staticmethod
    def _detect_source_type(request: ExtractionRequest) -> str:
        if request.source_type is not None:
            return request.source_type

        if request.url is None:
            return "telegram"

        url = str(request.url)
        parsed = urlparse(url)
        host = parsed.netloc.lower()
        path = parsed.path.lower()

        if "youtube.com" in host or "youtu.be" in host:
            return "youtube"

        if "instagram.com" in host:
            return "instagram"

        if path.endswith(".pdf"):
            return "pdf"

        if path.endswith((".png", ".jpg", ".jpeg", ".webp", ".gif")):
            return "image"

        return "article"

    @staticmethod
    def _is_instagram_reel(url: str) -> bool:
        parsed = urlparse(url)
        path = parsed.path.lower()
        host = parsed.netloc.lower()
        return "instagram.com" in host and ("/reel/" in path or "/reels/" in path)
