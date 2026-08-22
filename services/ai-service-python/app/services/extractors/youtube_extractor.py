from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qs, urlencode, urlparse
from urllib.request import Request, urlopen

from app.contracts.extractions import (
    ExtractionMetadata,
    ExtractionRequest,
    ExtractionResponse,
)
from app.services.extractors.youtube_transcript_provider import YouTubeTranscriptProvider


class YouTubeExtractor:
    OEMBED_ENDPOINT = "https://www.youtube.com/oembed"
    SUMMARY_TEXT_LIMIT = 18_000

    def __init__(self, transcript_provider: YouTubeTranscriptProvider | None = None) -> None:
        self._transcript_provider = transcript_provider or YouTubeTranscriptProvider()

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        source_type = request.source_type or "youtube"

        if request.url is None:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="video",
                extraction_status="failed",
                title=None,
                extracted_text=request.text.strip() if request.text else "",
                metadata=ExtractionMetadata(extra={"reason": "missing_url"}),
            )

        url = str(request.url)
        video_id = self._extract_video_id(url)

        if not video_id:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="video",
                extraction_status="failed",
                title=None,
                extracted_text=request.text.strip() if request.text else url,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    extra={"reason": "video_id_not_found"},
                ),
            )

        try:
            metadata = self._fetch_oembed(url)
            title = metadata.get("title")
            author_name = metadata.get("author_name")
            transcript = self._transcript_provider.fetch_transcript(video_id)

            extracted_text = self._build_extracted_text(title, author_name, url, video_id, transcript.text)

            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="video",
                extraction_status="completed",
                title=title,
                extracted_text=extracted_text,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    content_type="application/json",
                    final_url=url,
                    extra={
                        "video_id": video_id,
                        "author_name": author_name,
                        "provider_name": metadata.get("provider_name"),
                        "thumbnail_url": metadata.get("thumbnail_url"),
                        "transcript_status": transcript.status,
                        "transcript_language": transcript.language,
                        "transcript_language_name": transcript.language_name,
                        "transcript_is_generated": transcript.is_generated,
                        "transcript_reason": transcript.reason,
                    },
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError) as error:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="video",
                extraction_status="failed",
                title=None,
                extracted_text=request.text.strip() if request.text else url,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    extra={
                        "reason": str(error),
                        "video_id": video_id,
                        "transcript_status": "not_attempted",
                    },
                ),
            )

    def _fetch_oembed(self, url: str) -> dict[str, str]:
        query_string = urlencode({"url": url, "format": "json"})
        endpoint = f"{self.OEMBED_ENDPOINT}?{query_string}"
        request = Request(
            endpoint,
            headers={
                "User-Agent": "PersonalRagAgent/0.1 (+https://github.com/melihesensio99/personal-rag-agent)"
            },
        )

        with urlopen(request, timeout=10) as response:
            payload = response.read().decode("utf-8", errors="ignore")

        parsed = json.loads(payload)
        if not isinstance(parsed, dict):
            raise ValueError("invalid_oembed_payload")

        return parsed

    def _extract_video_id(self, url: str) -> str | None:
        parsed_url = urlparse(url)
        host = parsed_url.netloc.lower()

        if "youtu.be" in host:
            path = parsed_url.path.strip("/")
            return path or None

        if "youtube.com" in host:
            query = parse_qs(parsed_url.query)
            if "v" in query and query["v"]:
                return query["v"][0]

            path_parts = [part for part in parsed_url.path.split("/") if part]
            if len(path_parts) >= 2 and path_parts[0] in {"shorts", "embed", "live"}:
                return path_parts[1]

        return None

    def _build_extracted_text(
        self,
        title: str | None,
        author_name: str | None,
        url: str,
        video_id: str,
        transcript_text: str,
    ) -> str:
        if transcript_text.strip():
            return transcript_text[: self.SUMMARY_TEXT_LIMIT].strip()

        segments = []

        if title:
            segments.append(f"Video title: {title}.")

        if author_name:
            segments.append(f"Channel: {author_name}.")

        segments.append(f"Video id: {video_id}.")
        segments.append(f"Original URL: {url}")

        return " ".join(segments)[: self.SUMMARY_TEXT_LIMIT].strip()
