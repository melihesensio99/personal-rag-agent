from __future__ import annotations

import re
from html import unescape
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen

from app.contracts.extractions import (
    ExtractionMetadata,
    ExtractionRequest,
    ExtractionResponse,
)


class ArticleExtractor:
    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        if request.url is None:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=request.source_type,
                extraction_status="failed",
                title=None,
                extracted_text="",
                metadata=ExtractionMetadata(extra={"reason": "missing_url"}),
            )

        url = str(request.url)

        try:
            fetched = self._fetch_html(url)
            title = self._extract_title(fetched["html"])
            extracted_text = self._extract_text(fetched["html"])

            return ExtractionResponse(
                content_id=request.content_id,
                source_type=request.source_type,
                extraction_status="completed",
                title=title,
                extracted_text=extracted_text,
                original_url=url,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    content_type=fetched["content_type"],
                    final_url=fetched["final_url"],
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError) as error:
            return ExtractionResponse(
                content_id=request.content_id,
                source_type=request.source_type,
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
            raw_html = response.read().decode("utf-8", errors="ignore")
            final_url = response.geturl()

        return {
            "html": raw_html,
            "content_type": content_type,
            "final_url": final_url,
        }

    def _extract_title(self, html: str) -> str | None:
        match = re.search(r"<title[^>]*>(.*?)</title>", html, flags=re.IGNORECASE | re.DOTALL)
        if not match:
            return None

        title = self._normalize_text(match.group(1))
        return title or None

    def _extract_text(self, html: str) -> str:
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

        return normalized[:20000]

    def _normalize_text(self, value: str) -> str:
        collapsed = re.sub(r"\s+", " ", unescape(value)).strip()
        return collapsed
