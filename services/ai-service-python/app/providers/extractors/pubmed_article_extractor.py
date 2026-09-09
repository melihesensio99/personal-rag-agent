from __future__ import annotations

import re
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen
from xml.etree import ElementTree

from app.contracts.extractions import ExtractionMetadata, ExtractionRequest, ExtractionResponse, ReaderBlock


class PubMedArticleExtractor:
    """Extract PubMed title/abstract through NCBI E-utilities instead of web HTML."""

    PMID_PATTERN = re.compile(r"pubmed\.ncbi\.nlm\.nih\.gov/(\d+)", re.IGNORECASE)

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        url = str(request.url) if request.url else ""
        pmid = self._extract_pmid(url)
        if not pmid:
            return self._failed(request, url, "missing_pmid")

        try:
            xml = self._fetch_xml(pmid)
            root = ElementTree.fromstring(xml)
            article = root.find(".//PubmedArticle")
            if article is None:
                return self._failed(request, url, "article_not_found", pmid=pmid)

            title = self._node_text(article.find(".//ArticleTitle")) or f"PubMed article {pmid}"
            abstract = self._abstract_text(article)
            if not abstract:
                return self._failed(request, url, "abstract_unavailable", title=title, pmid=pmid)

            reader_blocks = self._build_reader_blocks(article, title)
            published = self._node_text(article.find(".//PubDate"))
            return ExtractionResponse(
                content_id=request.content_id,
                source_type="article",
                detected_content_kind="text",
                extraction_status="completed",
                title=title,
                extracted_text=abstract,
                original_url=url,
                reader_blocks=reader_blocks,
                metadata=ExtractionMetadata(
                    domain="pubmed.ncbi.nlm.nih.gov",
                    content_type="application/xml",
                    extra={"pmid": pmid, "published": published, "extractor": "ncbi_pubmed_xml", "scope": "abstract_only"},
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError, ElementTree.ParseError) as error:
            return self._failed(request, url, str(error), pmid=pmid)

    def _fetch_xml(self, pmid: str) -> str:
        endpoint = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?" + urlencode(
            {"db": "pubmed", "id": pmid, "retmode": "xml"}
        )
        request = Request(endpoint, headers={"User-Agent": "PersonalRagAgent/0.1"})
        with urlopen(request, timeout=20) as response:
            return response.read().decode("utf-8", errors="replace")

    @classmethod
    def _extract_pmid(cls, url: str) -> str | None:
        match = cls.PMID_PATTERN.search(url)
        return match.group(1) if match else None

    @staticmethod
    def _node_text(node: ElementTree.Element | None) -> str:
        return " ".join("".join(node.itertext()).split()) if node is not None else ""

    @classmethod
    def _abstract_text(cls, article: ElementTree.Element) -> str:
        parts = []
        for node in article.findall(".//Abstract/AbstractText"):
            label = node.attrib.get("Label")
            text = cls._node_text(node)
            if text:
                parts.append(f"{label}: {text}" if label else text)
        return "\n\n".join(parts)

    @classmethod
    def _build_reader_blocks(cls, article: ElementTree.Element, title: str) -> list[ReaderBlock]:
        blocks: list[ReaderBlock] = [ReaderBlock(type="heading", text=title, level=1)]
        for node in article.findall(".//Abstract/AbstractText"):
            label = node.attrib.get("Label")
            text = cls._node_text(node)
            if not text:
                continue
            if label:
                blocks.append(ReaderBlock(type="heading", text=label.title(), level=2))
            blocks.append(ReaderBlock(type="paragraph", text=text))
        return blocks

    @staticmethod
    def _failed(request: ExtractionRequest, url: str, reason: str, **extra: str) -> ExtractionResponse:
        return ExtractionResponse(
            content_id=request.content_id,
            source_type="article",
            detected_content_kind="text",
            extraction_status="failed",
            title=None,
            extracted_text="",
            original_url=url or None,
            metadata=ExtractionMetadata(domain="pubmed.ncbi.nlm.nih.gov", extra={"reason": reason, **extra}),
        )
