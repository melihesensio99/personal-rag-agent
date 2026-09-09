from __future__ import annotations

import re

from app.services.summary_input_preparer import SummaryInputPreparer
from html import unescape
from importlib import import_module
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qs, urljoin, urlparse
from urllib.request import Request, urlopen

from app.contracts.extractions import (
    ExtractionMetadata,
    ExtractionRequest,
    ExtractionResponse,
    ReaderBlock,
)


class ArticleExtractor:
    TEXT_LIMIT = 40_000

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
            image_url = self._extract_image_url(fetched["html"], fetched["final_url"] or url)
            extracted_text = self._extract_text(fetched["html"])
            reader_blocks = self._extract_reader_blocks(
                fetched["html"], fetched["final_url"] or url, title, image_url, extracted_text
            )
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
                reader_blocks=reader_blocks,
                metadata=ExtractionMetadata(
                    domain=urlparse(url).netloc,
                    content_type=fetched["content_type"],
                    final_url=fetched["final_url"],
                    extra={
                        "article_parser": self._resolve_parser_name(),
                        "image_url": image_url,
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
            return SummaryInputPreparer.prepare(trafilatura_text, max_chars=self.TEXT_LIMIT)

        without_scripts = re.sub(
            r"<(script|style)[^>]*>.*?</\1>",
            " ",
            html,
            flags=re.IGNORECASE | re.DOTALL,
        )
        without_scripts = re.sub(r"</(?:h[1-6]|p|li|div|section|article|blockquote)\s*>", "\n\n", without_scripts, flags=re.IGNORECASE)
        without_tags = re.sub(r"<[^>]+>", " ", without_scripts)
        normalized = SummaryInputPreparer.normalize_whitespace(unescape(without_tags)) if without_tags.strip() else ""

        if not normalized:
            raise ValueError("article_text_empty")

        return SummaryInputPreparer.prepare(normalized, max_chars=self.TEXT_LIMIT)

    def _extract_image_url(self, html: str, base_url: str) -> str | None:
        patterns = (
            r'<meta[^>]+(?:property|name)=["\']og:image["\'][^>]+content=["\']([^"\']+)',
            r'<meta[^>]+content=["\']([^"\']+)["\'][^>]+(?:property|name)=["\']og:image["\']',
            r'<meta[^>]+(?:property|name)=["\']twitter:image(?::src)?["\'][^>]+content=["\']([^"\']+)',
            r'<article[^>]*>.*?<img[^>]+src=["\']([^"\']+)',
        )
        for pattern in patterns:
            match = re.search(pattern, html, flags=re.IGNORECASE | re.DOTALL)
            if match:
                candidate = unescape(match.group(1)).strip()
                if candidate and not candidate.startswith("data:"):
                    return urljoin(base_url, candidate)
        return None

    def _extract_reader_blocks(
        self,
        html: str,
        base_url: str,
        title: str | None,
        image_url: str | None,
        extracted_text: str,
    ) -> list[ReaderBlock]:
        blocks = self._extract_reader_blocks_from_xml(html, base_url, title, image_url)
        if blocks is not None:
            return blocks

        return self._extract_reader_blocks_fallback(html, base_url, title, image_url, extracted_text)

    def _extract_reader_blocks_from_xml(
        self,
        html: str,
        base_url: str,
        title: str | None,
        image_url: str | None,
    ) -> list[ReaderBlock] | None:
        trafilatura_module = self._load_trafilatura()
        if trafilatura_module is None:
            return None

        xml_str = trafilatura_module.extract(
            html,
            output_format="xml",
            include_comments=False,
            include_links=False,
            include_images=True,
            favor_precision=True,
        )
        if not isinstance(xml_str, str) or not xml_str.strip():
            return None

        try:
            from xml.etree import ElementTree
            root = ElementTree.fromstring(xml_str)
        except Exception:
            return None

        blocks: list[ReaderBlock] = []
        if title:
            blocks.append(ReaderBlock(type="heading", text=title, level=1))
        if image_url:
            blocks.append(ReaderBlock(type="image", url=image_url, caption=title))

        consumed = 0
        for node in root.iter():
            tag = node.tag.rsplit("}", 1)[-1] if "}" in node.tag else node.tag

            if tag == "head":
                text = self._normalize_text(" ".join(node.itertext()))
                if not text or (title and text.casefold() == title.casefold()):
                    continue
                rend = node.attrib.get("rend", "")
                level = 2
                if rend.startswith("h") and rend[1:].isdigit():
                    level = max(1, min(6, int(rend[1:])))
                blocks.append(ReaderBlock(type="heading", text=text, level=level))

            elif tag == "p":
                text = self._normalize_text(" ".join(node.itertext()))
                if not text or len(text) < 20:
                    continue
                consumed += len(text)
                if consumed > self.TEXT_LIMIT:
                    break
                blocks.append(ReaderBlock(type="paragraph", text=text))

            elif tag == "list":
                items: list[str] = []
                for item_node in node:
                    item_tag = item_node.tag.rsplit("}", 1)[-1] if "}" in item_node.tag else item_node.tag
                    if item_tag == "item":
                        item_text = self._normalize_text(" ".join(item_node.itertext()))
                        if item_text:
                            items.append(item_text)
                if items:
                    consumed += sum(len(item) for item in items)
                    if consumed > self.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="list", items=items))

            elif tag == "quote":
                text = self._normalize_text(" ".join(node.itertext()))
                if text:
                    consumed += len(text)
                    if consumed > self.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="quote", text=text))

            elif tag == "graphic":
                graphic_url = node.attrib.get("src") or node.attrib.get("url")
                if graphic_url and not graphic_url.startswith("data:"):
                    resolved_url = urljoin(base_url, graphic_url)
                    if not image_url or resolved_url != image_url:
                        caption = node.attrib.get("title") or node.attrib.get("alt")
                        blocks.append(ReaderBlock(type="image", url=resolved_url, caption=caption))

        if not any(block.type == "paragraph" for block in blocks):
            return None

        return blocks[:300]

    def _extract_reader_blocks_fallback(
        self,
        html: str,
        base_url: str,
        title: str | None,
        image_url: str | None,
        extracted_text: str,
    ) -> list[ReaderBlock]:
        blocks: list[ReaderBlock] = []
        if title:
            blocks.append(ReaderBlock(type="heading", text=title, level=1))
        if image_url:
            blocks.append(ReaderBlock(type="image", url=image_url, caption=title))

        article_match = re.search(r"<(?:article|main)\b[^>]*>(.*?)</(?:article|main)>", html, re.IGNORECASE | re.DOTALL)
        readable_html = article_match.group(1) if article_match else html

        token_pattern = re.compile(r"<(h[1-6]|p|ul|ol|blockquote)\b[^>]*>(.*?)</\1>", re.IGNORECASE | re.DOTALL)
        consumed = 0
        for match in token_pattern.finditer(readable_html):
            tag = match.group(1).lower()
            inner = match.group(2)

            if tag.startswith("h"):
                text = self._normalize_text(re.sub(r"<[^>]+>", " ", inner))
                if not text or (title and text.casefold() == title.casefold()):
                    continue
                consumed += len(text)
                if consumed > self.TEXT_LIMIT:
                    break
                blocks.append(ReaderBlock(type="heading", text=text, level=int(tag[1])))

            elif tag == "p":
                text = self._normalize_text(re.sub(r"<[^>]+>", " ", inner))
                if not text or len(text) < 20:
                    continue
                consumed += len(text)
                if consumed > self.TEXT_LIMIT:
                    break
                blocks.append(ReaderBlock(type="paragraph", text=text))

            elif tag in ("ul", "ol"):
                items = [
                    self._normalize_text(re.sub(r"<[^>]+>", " ", li_match.group(1)))
                    for li_match in re.finditer(r"<li[^>]*>(.*?)</li>", inner, re.IGNORECASE | re.DOTALL)
                ]
                items = [item for item in items if item]
                if items:
                    consumed += sum(len(item) for item in items)
                    if consumed > self.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="list", items=items))

            elif tag == "blockquote":
                text = self._normalize_text(re.sub(r"<[^>]+>", " ", inner))
                if text:
                    consumed += len(text)
                    if consumed > self.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="quote", text=text))

        if not any(block.type == "paragraph" for block in blocks):
            for paragraph in re.split(r"\n\s*\n", extracted_text):
                text = self._normalize_text(paragraph)
                if text:
                    blocks.append(ReaderBlock(type="paragraph", text=text))

        return blocks[:300]

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

        normalized = SummaryInputPreparer.normalize_whitespace(extracted) if extracted.strip() else ""
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

        if content_type_lower.startswith("video/"):
            return "video"

        if content_type_lower.startswith("text/html"):
            return "text"

        return "unknown"

    def _normalize_text(self, value: str) -> str:
        collapsed = re.sub(r"\s+", " ", unescape(value)).strip()
        return collapsed
