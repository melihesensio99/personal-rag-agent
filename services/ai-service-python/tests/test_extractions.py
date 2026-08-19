from fastapi.testclient import TestClient

from app.main import app
from app.services.extractors.article_extractor import ArticleExtractor
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
    assert "Transcript: Controllers should stay thin." in body["extracted_text"]
    assert body["metadata"]["extra"]["transcript_status"] == "completed"
    assert body["metadata"]["extra"]["transcript_language"] == "en"


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
