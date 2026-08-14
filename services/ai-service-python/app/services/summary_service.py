import re

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.prompt_loader import PromptLoader


class SummaryService:
    def __init__(self, prompt_loader: PromptLoader) -> None:
        self._prompt_loader = prompt_loader

    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        normalized_text = self._normalize_whitespace(request.text)
        sentences = self._split_sentences(normalized_text)

        title = self._build_title(sentences, normalized_text)
        short_summary = self._build_short_summary(sentences, normalized_text)
        key_points = self._build_key_points(sentences, normalized_text)
        tags = self._build_tags(normalized_text)
        language = self._detect_language(normalized_text)

        return SummaryResponse(
            content_id=request.content_id,
            title=title,
            short_summary=short_summary,
            key_points=key_points,
            tags=tags,
            language=language,
            provider="fake",
        )

    def _build_title(self, sentences: list[str], fallback_text: str) -> str:
        candidate = sentences[0] if sentences else fallback_text
        trimmed = candidate[:60].strip(" .,:;-")
        return trimmed or "Untitled content"

    def _build_short_summary(self, sentences: list[str], fallback_text: str) -> str:
        if len(sentences) >= 2:
            return " ".join(sentences[:2])[:280].strip()

        return fallback_text[:280].strip()

    def _build_key_points(self, sentences: list[str], fallback_text: str) -> list[str]:
        candidates = sentences[:3] if sentences else [fallback_text]
        return [sentence[:140].strip() for sentence in candidates if sentence.strip()]

    def _build_tags(self, text: str) -> list[str]:
        words = re.findall(r"[A-Za-zÀ-ÿ0-9']+", text.lower())
        stop_words = {
            "the",
            "and",
            "for",
            "with",
            "that",
            "this",
            "from",
            "video",
            "about",
            "bir",
            "ve",
            "ile",
            "icin",
            "gibi",
            "daha",
            "olarak",
            "kadar",
        }

        unique_words: list[str] = []
        for word in words:
            if len(word) < 4 or word in stop_words or word in unique_words:
                continue
            unique_words.append(word)

        if not unique_words:
            return ["general"]

        return unique_words[:4]

    def _detect_language(self, text: str) -> str:
        turkish_markers = (" bir ", " ve ", " için ", " olan ", " olarak ")
        normalized = f" {text.lower()} "
        return "tr" if any(marker in normalized for marker in turkish_markers) else "en"

    def _split_sentences(self, text: str) -> list[str]:
        return [segment.strip() for segment in re.split(r"(?<=[.!?])\s+", text) if segment.strip()]

    def _normalize_whitespace(self, text: str) -> str:
        condensed = " ".join(text.split())
        if not condensed:
            raise ValueError("Summary input cannot be empty.")

        self._prompt_loader.load()
        return condensed
