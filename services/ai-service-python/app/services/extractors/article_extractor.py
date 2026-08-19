from __future__ import annotations

import re
from html import unescape
from importlib import import_module
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qs, urlparse
from urllib.request import Request, urlopen

from app.contracts.extractions import (
    ExtractionMetadata,
    ExtractionRequest,
    ExtractionResponse,
)


class ArticleExtractor:
    TEXT_LIMIT = 20_000

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        source_type = request.source_type or "article"

        if request.url is None:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="unknown",
                extraction_status="failed",
                title=None,
                extracted_text="",
                metadata=ExtractionMetadata(extra={"reason": "missing_url"}),
            )

        url = str(request.url)

        if self._is_google_search_url(url):
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind="unknown",
                extraction_status="unsupported",
                title="Google search result page",
                extracted_text=request.text.strip() if request.text else url,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    extra={
                        "reason": "search_result_page",
                        "message": "Send the actual article/video/PDF URL instead of a Google search result URL.",
                    },
                ),
            )

        try:
            fetched = self._fetch_html(url)
            title = self._extract_title(fetched["html"])
            extracted_text = self._extract_text(fetched["html"])
            detected_content_kind = self._detect_content_kind(
                url=url,
                final_url=fetched["final_url"] or url,
                html=fetched["html"],
                content_type=fetched["content_type"],
            )

            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind=detected_content_kind,
                extraction_status="completed",
                title=title,
                extracted_text=extracted_text,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    content_type=fetched["content_type"],
                    final_url=fetched["final_url"],
                    extra={
                        "article_parser": self._resolve_parser_name(),
                    },
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError) as error:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=source_type,
                detected_content_kind=self._detect_content_kind(
                    url=url,
                    final_url=url,
                    html=request.text or "",
                    content_type=None,
                ),
                extraction_status="failed",
                title=None,
                extracted_text=request.text.strip() if request.text else "",
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    extra={"reason": str(error)},
                ),
            )

    def _fetch_html(self, url: str) -> dict[str, str | None]:
        request = Request(
            url,
            headers={
                "User-Agent": "PersonalRagAgent/0.1 (+https://github.com/melihesensio99/personal-rag-agent)"
            },
        )

        with urlopen(request, timeout=10) as response:
            content_type = response.headers.get("Content-Type")
            raw_html = response.read().decode(response.headers.get_content_charset() or "utf-8", errors="replace")
            final_url = response.geturl()

        return {
            "html": raw_html,
            "content_type": content_type,
            "final_url": final_url,
        }

    def _extract_title(self, html: str) -> str | None:
        trafilatura_module = self._load_trafilatura()
        if trafilatura_module is not None and hasattr(trafilatura_module, "extract_metadata"):
            metadata = trafilatura_module.extract_metadata(html)
            trafilatura_title = getattr(metadata, "title", None)
            if isinstance(trafilatura_title, str) and trafilatura_title.strip():
                return self._normalize_text(trafilatura_title)

        match = re.search(r"<title[^>]*>(.*?)</title>", html, flags=re.IGNORECASE | re.DOTALL)
        if not match:
            return None

        title = self._normalize_text(match.group(1))
        return title or None

    def _extract_text(self, html: str) -> str:
        trafilatura_text = self._extract_with_trafilatura(html)
        if trafilatura_text:
            return trafilatura_text[: self.TEXT_LIMIT]

        without_scripts = re.sub(
            r"<(script|style)[^>]*>.*?</\1>",
            " ",
            html,
            flags=re.IGNORECASE | re.DOTALL,
        )
        without_tags = re.sub(r"<[^>]+>", " ", without_scripts)
        normalized = self._normalize_text(without_tags)

        if not normalized:
            raise ValueError("article_text_empty")

        return normalized[: self.TEXT_LIMIT]

    def _extract_with_trafilatura(self, html: str) -> str | None:
        trafilatura_module = self._load_trafilatura()
        if trafilatura_module is None:
            return None

        extracted = trafilatura_module.extract(
            html,
            output_format="markdown",
            include_comments=False,
            include_links=False,
            include_images=False,
            favor_precision=True,
        )

        if not isinstance(extracted, str):
            return None

        normalized = self._normalize_text(extracted)
        return normalized or None

    def _load_trafilatura(self) -> Any | None:
        try:
            return import_module("trafilatura")
        except ModuleNotFoundError:
            return None

    def _resolve_parser_name(self) -> str:
        return "trafilatura" if self._load_trafilatura() is not None else "html_regex_fallback"

    def _is_google_search_url(self, url: str) -> bool:
        parsed = urlparse(url)
        host = parsed.netloc.lower()
        path = parsed.path.lower()
        query = parse_qs(parsed.query)

        return host.endswith("google.com") and path.startswith("/search") and "q" in query

    def _detect_content_kind(
        self,
        url: str,
        final_url: str,
        html: str,
        content_type: str | None,
    ) -> str:
        normalized_html = html.lower()
        candidate_url = final_url or url
        parsed = urlparse(candidate_url)
        host = parsed.netloc.lower()
        path = parsed.path.lower()
        content_type_lower = (content_type or "").lower()

        if host.endswith("youtube.com") or host.endswith("youtu.be"):
            return "video"

        known_video_hosts = (
            "dailymotion.com",
            "vimeo.com",
            "tiktok.com",
            "twitch.tv",
            "loom.com",
            "wistia.com",
            "jwplayer.com",
        )

        if any(video_host in host for video_host in known_video_hosts):
            return "video"

        if "instagram.com" in host and ("/reel/" in path or "/reels/" in path):
            return "video"

        if content_type_lower.startswith("image/"):
            return "image"

        metadata_video_signals = (
            'property="og:type" content="video',
            "property='og:type' content='video",
            'name="twitter:player"',
            "name='twitter:player'",
            "<video",
            "application/ld+json",
            "videoobject",
            "player.vimeo.com/video/",
            "dailymotion.com/embed/video/",
            "youtube.com/embed/",
        )

        if any(signal in normalized_html for signal in metadata_video_signals):
            return "video"

        if content_type_lower.startswith("text/html"):
            return "text"

        return "unknown"

    def _normalize_text(self, value: str) -> str:
        collapsed = re.sub(r"\s+", " ", unescape(value)).strip()
        return collapsed
