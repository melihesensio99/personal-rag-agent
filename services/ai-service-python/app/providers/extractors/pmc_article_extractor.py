from __future__ import annotations

import re

from app.services.summary_input_preparer import SummaryInputPreparer
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from xml.etree import ElementTree

from app.contracts.extractions import ExtractionMetadata, ExtractionRequest, ExtractionResponse, ReaderBlock


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
            is_bioc = root.tag.rsplit("}", 1)[-1] == "collection"
            title = self._first_text(root, "article-title") or pmc_id
            text = self._article_text(root)
            image_url = self._first_figure_url(root, url)
            reader_blocks = self._reader_blocks(root, url, title, is_bioc)
            if len(text) < 100:
                return self._failed(request, url, "empty_or_incomplete_pmc_text", title=title)

            return ExtractionResponse(
                content_id=request.content_id,
                source_type="article",
                detected_content_kind="text",
                extraction_status="completed",
                title=title,
                extracted_text=SummaryInputPreparer.prepare(text, max_chars=self.TEXT_LIMIT),
                original_url=url,
                reader_blocks=reader_blocks,
                metadata=ExtractionMetadata(
                    domain="pmc.ncbi.nlm.nih.gov",
                    content_type="application/xml",
                    extra={"pmc_id": pmc_id, "extractor": "ncbi_bioc_xml" if is_bioc else "europe_pmc_xml", "image_url": image_url},
                ),
            )
        except (HTTPError, URLError, TimeoutError, ValueError, ElementTree.ParseError) as error:
            return self._failed(request, url, str(error), pmc_id=pmc_id)

    def _fetch_xml(self, pmc_id: str) -> str:
        endpoint = f"https://www.ebi.ac.uk/europepmc/webservices/rest/{pmc_id}/fullTextXML"
        request = Request(endpoint, headers={"User-Agent": "PersonalRagAgent/0.1"})
        try:
            with urlopen(request, timeout=20) as response:
                return response.read().decode("utf-8", errors="replace")
        except HTTPError as error:
            if error.code != 404:
                raise
            fallback_endpoint = f"https://www.ncbi.nlm.nih.gov/research/bionlp/RESTful/pmcoa.cgi/BioC_xml/{pmc_id}/unicode"
            fallback_request = Request(fallback_endpoint, headers={"User-Agent": "PersonalRagAgent/0.1"})
            with urlopen(fallback_request, timeout=20) as response:
                return response.read().decode("utf-8", errors="replace")

    @classmethod
    def _extract_pmc_id(cls, url: str) -> str | None:
        match = cls.PMC_ID_PATTERN.search(url)
        return match.group(1).upper() if match else None

    @staticmethod
    def _first_text(root: ElementTree.Element, tag: str) -> str:
        node = root.find(f".//{tag}")
        if node is not None:
            return " ".join("".join(node.itertext()).split())
        for passage in root.findall(".//passage"):
            if any(infon.attrib.get("key") == "section_type" and (infon.text or "").upper() == "TITLE" for infon in passage.findall("infon")):
                text_node = passage.find("text")
                return " ".join((text_node.text or "").split()) if text_node is not None else ""
        return ""

    @staticmethod
    def _article_text(root: ElementTree.Element) -> str:
        parts: list[str] = []

        def visit(node: ElementTree.Element) -> None:
            tag = node.tag.rsplit("}", 1)[-1]
            if tag in {"ref-list", "ack"}:
                return
            if tag in {"title", "p", "table", "list-item", "disp-quote"}:
                value = " ".join("".join(node.itertext()).split())
                if value:
                    parts.append(f"## {value}" if tag == "title" else value)
                return
            if node.text and node.text.strip():
                parts.append(" ".join(node.text.split()))
            for child in node:
                visit(child)
                if child.tail and child.tail.strip():
                    parts.append(" ".join(child.tail.split()))

        for node in root.findall(".//abstract") + root.findall(".//body"):
            if node.tag == "abstract":
                parts.append("## Abstract")
            visit(node)
        if parts:
            return "\n\n".join(parts)
        current_section = ""
        for passage in root.findall(".//passage"):
            section = next(((infon.text or "").strip() for infon in passage.findall("infon")
                            if infon.attrib.get("key") == "section_type"), "")
            if section.upper() in {"REF", "REFERENCES", "ACK", "ACK_FUND"}:
                continue
            text_node = passage.find("text")
            text = " ".join((text_node.text or "").split()) if text_node is not None else ""
            if text:
                if section and section != current_section:
                    parts.append(f"## {section}")
                    current_section = section
                parts.append(text)
        return "\n\n".join(parts)

    @staticmethod
    def _first_figure_url(root: ElementTree.Element, article_url: str) -> str | None:
        for node in root.iter():
            if node.tag.rsplit("}", 1)[-1] != "graphic":
                continue
            href = next((value for key, value in node.attrib.items() if key.rsplit("}", 1)[-1] == "href"), None)
            if href:
                return f"{article_url.rstrip('/')}/bin/{href.lstrip('/')}"
        return None

    @classmethod
    def _reader_blocks(
        cls,
        root: ElementTree.Element,
        article_url: str,
        title: str,
        is_bioc: bool,
    ) -> list[ReaderBlock]:
        blocks = [ReaderBlock(type="heading", text=title, level=1)]
        consumed = len(title)

        if is_bioc:
            current_section = ""
            for passage in root.findall(".//passage"):
                section = next((infon.text or "" for infon in passage.findall("infon") if infon.attrib.get("key") == "section_type"), "").strip()
                text_node = passage.find("text")
                text = " ".join((text_node.text or "").split()) if text_node is not None else ""
                if not text or section.upper() == "TITLE":
                    continue
                if section and section.casefold() != current_section.casefold():
                    blocks.append(ReaderBlock(type="heading", text=section.title(), level=2))
                    current_section = section
                consumed += len(text)
                if consumed > cls.TEXT_LIMIT:
                    break
                blocks.append(ReaderBlock(type="paragraph", text=text))
            return blocks[:300]

        for node in root.iter():
            tag = node.tag.rsplit("}", 1)[-1]
            if tag in {"ref-list", "ref", "ack", "fn-group"}:
                continue
            if tag == "title":
                text = " ".join("".join(node.itertext()).split())
                if text and text.casefold() != title.casefold():
                    blocks.append(ReaderBlock(type="heading", text=text, level=2))
            elif tag == "p":
                text = " ".join("".join(node.itertext()).split())
                if text:
                    consumed += len(text)
                    if consumed > cls.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="paragraph", text=text))
            elif tag == "list":
                items: list[str] = []
                for item_node in node:
                    item_tag = item_node.tag.rsplit("}", 1)[-1]
                    if item_tag == "list-item":
                        item_text = " ".join("".join(item_node.itertext()).split())
                        if item_text:
                            items.append(item_text)
                if items:
                    consumed += sum(len(item) for item in items)
                    if consumed > cls.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="list", items=items))
            elif tag == "disp-quote":
                text = " ".join("".join(node.itertext()).split())
                if text:
                    consumed += len(text)
                    if consumed > cls.TEXT_LIMIT:
                        break
                    blocks.append(ReaderBlock(type="quote", text=text))
            elif tag == "graphic":
                href = next((value for key, value in node.attrib.items() if key.rsplit("}", 1)[-1] == "href"), None)
                if href:
                    blocks.append(ReaderBlock(type="image", url=f"{article_url.rstrip('/')}/bin/{href.lstrip('/')}", caption="Makale görseli"))
        return blocks[:300]

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
