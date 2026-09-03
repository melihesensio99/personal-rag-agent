from __future__ import annotations

import re
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from xml.etree import ElementTree

from app.contracts.extractions import ExtractionMetadata, ExtractionRequest, ExtractionResponse


class PmcArticleExtractor:
    """Extract full text from PubMed Central through Europe PMC's XML endpoint."""

    PMC_ID_PATTERN = re.compile(r"/(PMC\d+)(?:/|$)", re.IGNORECASE)
    TEXT_LIMIT = 50_000

    def extract(self, request: ExtractionRequest) -> ExtractionResponse:
        url = str(request.url) if request.url else ""
        pmc_id = self._extract_pmc_id(url)
        if not pmc_id:
            return self._failed(request, url, "missing_pmc_id")

        try:
            xml = self._fetch_xml(pmc_id)
            root = ElementTree.fromstring(xml)
            title = self._first_text(root, "article-title") or pmc_id
            text = self._article_text(root)
            if len(text) < 100:
                return self._failed(request, url, "empty_or_incomplete_pmc_text", title=title)

            return ExtractionResponse(
                content_id=request.content_id,
                source_type="article",
                detected_content_kind="text",
                extraction_status="completed",
                title=title,
                extracted_text=text[: self.TEXT_LIMIT],
                original_url=url,
                metadata=ExtractionMetadata(
                    domain="pmc.ncbi.nlm.nih.gov",
                    content_type="application/xml",
                    extra={"pmc_id": pmc_id, "extractor": "europe_pmc_xml"},
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError, ElementTree.ParseError) as error:
            return self._failed(request, url, str(error), pmc_id=pmc_id)

    def _fetch_xml(self, pmc_id: str) -> str:
        endpoint = f"https://www.ebi.ac.uk/europepmc/webservices/rest/{pmc_id}/fullTextXML"
        request = Request(endpoint, headers={"User-Agent": "PersonalRagAgent/0.1"})
        with urlopen(request, timeout=20) as response:
            return response.read().decode("utf-8", errors="replace")

    @classmethod
    def _extract_pmc_id(cls, url: str) -> str | None:
        match = cls.PMC_ID_PATTERN.search(url)
        return match.group(1).upper() if match else None

    @staticmethod
    def _first_text(root: ElementTree.Element, tag: str) -> str:
        node = root.find(f".//{tag}")
        return " ".join("".join(node.itertext()).split()) if node is not None else ""

    @staticmethod
    def _article_text(root: ElementTree.Element) -> str:
        parts: list[str] = []
        for node in root.findall(".//abstract") + root.findall(".//body"):
            text = " ".join("".join(node.itertext()).split())
            if text:
                parts.append(text)
        return "\n\n".join(parts)

    @staticmethod
    def _failed(request: ExtractionRequest, url: str, reason: str, **extra: str) -> ExtractionResponse:
        metadata = {"reason": reason, **extra}
        return ExtractionResponse(
            content_id=request.content_id,
            source_type="article",
            detected_content_kind="text",
            extraction_status="failed",
            title=None,
            extracted_text="",
            original_url=url or None,
            metadata=ExtractionMetadata(domain="pmc.ncbi.nlm.nih.gov", extra=metadata),
        )
