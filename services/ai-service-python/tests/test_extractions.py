from fastapi.testclient import TestClient

from app.main import app
from app.services.extractors.article_extractor import ArticleExtractor
from app.services.extractors.pmc_article_extractor import PmcArticleExtractor
from app.services.extractors.pubmed_article_extractor import PubMedArticleExtractor
from app.services.extractors.youtube_extractor import YouTubeExtractor
from app.services.extractors.youtube_transcript_provider import YouTubeTranscriptProvider, YouTubeTranscriptResult

client = TestClient(app)


def test_create_article_extraction_returns_cleaned_text(monkeypatch) -> None:
    def fake_fetch_html(self: ArticleExtractor, url: str) -> dict[str, str | None]:
        return {
            "html": """
                <html>
                    <head><title>RAG Guide</title></head>
                    <body>
                        <article>
                            <h1>RAG Guide</h1>
                            <p>Retrieval augmented generation combines search and generation.</p>
                            <p>Chunking quality affects retrieval accuracy.</p>
                        </article>
                    </body>
                </html>
            """,
            "content_type": "text/html",
            "final_url": url,
        }

    monkeypatch.setattr(ArticleExtractor, "_fetch_html", fake_fetch_html)

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-article-1",
            "source_type": "article",
            "url": "https://example.com/rag-guide",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "content-article-1"
    assert body["source_type"] == "article"
    assert body["detected_content_kind"] == "text"
    assert body["extraction_status"] == "completed"
    assert body["title"] == "RAG Guide"
    assert "Retrieval augmented generation combines search and generation." in body["extracted_text"]
    assert body["metadata"]["domain"] == "example.com"


def test_create_article_extraction_prefers_trafilatura_when_available(monkeypatch) -> None:
    class FakeMetadata:
        title = "Clean RAG Article"

    class FakeTrafilatura:
        @staticmethod
        def extract_metadata(html: str) -> FakeMetadata:
            return FakeMetadata()

        @staticmethod
        def extract(
            html: str,
            output_format: str,
            include_comments: bool,
            include_links: bool,
            include_images: bool,
            favor_precision: bool,
        ) -> str:
            return "Main article body about retrieval augmented generation."

    def fake_fetch_html(self: ArticleExtractor, url: str) -> dict[str, str | None]:
        return {
            "html": """
                <html>
                    <head><title>Noisy fallback title</title></head>
                    <body>
                        <nav>Subscribe now</nav>
                        <article><p>Main article body about retrieval augmented generation.</p></article>
                    </body>
                </html>
            """,
            "content_type": "text/html",
            "final_url": url,
        }

    monkeypatch.setattr(ArticleExtractor, "_fetch_html", fake_fetch_html)
    monkeypatch.setattr(ArticleExtractor, "_load_trafilatura", lambda self: FakeTrafilatura)

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-article-trafilatura-1",
            "url": "https://example.com/clean-rag",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["title"] == "Clean RAG Article"
    assert body["extracted_text"] == "Main article body about retrieval augmented generation."
    assert body["metadata"]["extra"]["article_parser"] == "trafilatura"


def test_create_article_extraction_rejects_google_search_page() -> None:
    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-google-search-1",
            "url": "https://www.google.com/search?q=RAG+Mimarisi",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "article"
    assert body["detected_content_kind"] == "unknown"
    assert body["extraction_status"] == "unsupported"
    assert body["metadata"]["extra"]["reason"] == "search_result_page"


def test_create_pmc_extraction_uses_full_text_xml(monkeypatch) -> None:
    monkeypatch.setattr(
        PmcArticleExtractor,
        "_fetch_xml",
        lambda self, pmc_id: """<article><front><article-meta><article-title>PMC Study</article-title></article-meta></front>
            <abstract><p>Abstract about low carbohydrate diets and diabetes outcomes.</p></abstract>
            <body><sec><title>Results</title><p>This is the full text body with enough detail to be indexed and searched reliably.</p></sec></body>
        </article>""",
    )

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-pmc-1",
            "url": "https://pmc.ncbi.nlm.nih.gov/articles/PMC6566854/",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "article"
    assert body["extraction_status"] == "completed"
    assert body["title"] == "PMC Study"
    assert "low carbohydrate diets" in body["extracted_text"]
    assert body["metadata"]["extra"]["pmc_id"] == "PMC6566854"


def test_create_pubmed_extraction_uses_ncbi_xml(monkeypatch) -> None:
    monkeypatch.setattr(
        PubMedArticleExtractor,
        "_fetch_xml",
        lambda self, pmid: """<PubmedArticleSet><PubmedArticle><MedlineCitation><Article>
            <ArticleTitle>PubMed Study</ArticleTitle><Abstract>
            <AbstractText Label=\"BACKGROUND\">Abstract about nutrition and diabetes.</AbstractText>
            <AbstractText>Results support further research.</AbstractText>
            </Abstract></Article></MedlineCitation></PubmedArticle></PubmedArticleSet>""",
    )

    response = client.post(
        "/api/v1/extractions",
        json={"content_id": "content-pubmed-1", "url": "https://pubmed.ncbi.nlm.nih.gov/19049813/"},
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "article"
    assert body["extraction_status"] == "completed"
    assert body["title"] == "PubMed Study"
    assert "BACKGROUND: Abstract about nutrition" in body["extracted_text"]
    assert body["metadata"]["extra"]["pmid"] == "19049813"


def test_create_extraction_detects_source_type_when_not_provided(monkeypatch) -> None:
    def fake_fetch_html(self: ArticleExtractor, url: str) -> dict[str, str | None]:
        return {
            "html": """
                <html>
                    <head><title>Daily Motion Clip</title><meta property="og:type" content="video.other" /></head>
                    <body><div>Video page</div></body>
                </html>
            """,
            "content_type": "text/html",
            "final_url": url,
        }

    monkeypatch.setattr(ArticleExtractor, "_fetch_html", fake_fetch_html)

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-auto-1",
            "url": "https://www.dailymotion.com/video/xayigze",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "article"
    assert body["detected_content_kind"] == "video"


def test_article_with_embedded_video_metadata_stays_text(monkeypatch) -> None:
    def fake_fetch_html(self: ArticleExtractor, url: str) -> dict[str, str | None]:
        return {
            "html": """
                <html>
                    <head>
                        <title>What is stimming?</title>
                        <script type="application/ld+json">
                            {"@type":"VideoObject","name":"Embedded explainer"}
                        </script>
                    </head>
                    <body>
                        <article>
                            <p>Stimming can help people regulate emotions and sensory input.</p>
                        </article>
                    </body>
                </html>
            """,
            "content_type": "text/html",
            "final_url": url,
        }

    monkeypatch.setattr(ArticleExtractor, "_fetch_html", fake_fetch_html)

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-article-embedded-video-1",
            "url": "https://health.clevelandclinic.org/what-is-stimming",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "article"
    assert body["detected_content_kind"] == "text"


def test_create_youtube_extraction_returns_oembed_metadata(monkeypatch) -> None:
    def fake_fetch_oembed(self: YouTubeExtractor, url: str) -> dict[str, str]:
        return {
            "title": "RAG vs Semantic Search",
            "author_name": "Melih Labs",
            "provider_name": "YouTube",
            "thumbnail_url": "https://img.youtube.com/vi/abc123/maxresdefault.jpg",
        }

    monkeypatch.setattr(YouTubeExtractor, "_fetch_oembed", fake_fetch_oembed)
    monkeypatch.setattr(
        YouTubeTranscriptProvider,
        "fetch_transcript",
        lambda self, video_id: YouTubeTranscriptResult(status="unavailable", reason="NoTranscriptFound"),
    )

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-youtube-1",
            "source_type": "youtube",
            "url": "https://www.youtube.com/watch?v=abc123",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["detected_content_kind"] == "video"
    assert body["extraction_status"] == "completed"
    assert body["title"] == "RAG vs Semantic Search"
    assert "Channel: Melih Labs." in body["extracted_text"]
    assert body["metadata"]["extra"]["video_id"] == "abc123"
    assert body["metadata"]["extra"]["transcript_status"] == "unavailable"


def test_create_youtube_extraction_includes_transcript_when_available(monkeypatch) -> None:
    def fake_fetch_oembed(self: YouTubeExtractor, url: str) -> dict[str, str]:
        return {
            "title": "Clean Architecture Lessons",
            "author_name": "Backend Notes",
            "provider_name": "YouTube",
            "thumbnail_url": "https://img.youtube.com/vi/clean123/maxresdefault.jpg",
        }

    monkeypatch.setattr(YouTubeExtractor, "_fetch_oembed", fake_fetch_oembed)
    monkeypatch.setattr(
        YouTubeTranscriptProvider,
        "fetch_transcript",
        lambda self, video_id: YouTubeTranscriptResult(
            status="completed",
            text="Controllers should stay thin. Business rules should live in application services.",
            language="en",
        ),
    )

    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-youtube-transcript-1",
            "url": "https://www.youtube.com/watch?v=clean123",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["source_type"] == "youtube"
    assert body["detected_content_kind"] == "video"
    assert body["extraction_status"] == "completed"
    assert body["extracted_text"] == "Controllers should stay thin. Business rules should live in application services."
    assert body["metadata"]["extra"]["transcript_status"] == "completed"
    assert body["metadata"]["extra"]["transcript_language"] == "en"


def test_youtube_transcript_provider_falls_back_to_available_language(monkeypatch) -> None:
    class FakeTranscript:
        language_code = "hi"

        def fetch(self) -> list[dict[str, str]]:
            return [
                {"text": "Retrieval augmented generation kya hota hai?", "language_code": "hi"},
                {"text": "Is video mein RAG samjhaya gaya hai.", "language_code": "hi"},
            ]

    class FakeTranscriptApi:
        def fetch(self, video_id: str, languages: list[str]) -> list[dict[str, str]]:
            raise RuntimeError("preferred languages were not available")

        def list(self, video_id: str) -> list[FakeTranscript]:
            return [FakeTranscript()]

    monkeypatch.setattr(
        YouTubeTranscriptProvider,
        "_load_transcript_api",
        lambda self: FakeTranscriptApi,
    )

    result = YouTubeTranscriptProvider().fetch_transcript("hindi-video-1")

    assert result.status == "completed"
    assert result.language == "hi"
    assert "RAG samjhaya gaya hai" in result.text


def test_create_unsupported_extraction_falls_back_to_text() -> None:
    response = client.post(
        "/api/v1/extractions",
        json={
            "content_id": "content-image-1",
            "url": "https://example.com/demo.png",
            "text": "https://example.com/demo.png",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["detected_content_kind"] == "image"
    assert body["extraction_status"] == "unsupported"
    assert body["extracted_text"] == "https://example.com/demo.png"
