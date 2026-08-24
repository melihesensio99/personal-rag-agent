from fastapi.testclient import TestClient

from app.dependencies import get_summary_service
from app.main import app
from app.services.prompt_loader import PromptLoader
from app.services.summary_providers.fake_summary_provider import FakeSummaryProvider
from app.services.summary_service import SummaryService


def _fake_summary_service() -> SummaryService:
    prompt_loader = PromptLoader("app/prompts/content_summary_v1.txt")
    return SummaryService(FakeSummaryProvider(prompt_loader))


app.dependency_overrides[get_summary_service] = _fake_summary_service

client = TestClient(app)


def test_create_summary_returns_structured_response() -> None:
    response = client.post(
        "/api/v1/summaries",
        json={
            "content_id": "content-123",
            "text": (
                "This video explains how retrieval augmented generation works. "
                "It covers embeddings, vector databases, and why chunking quality matters. "
                "The speaker also compares naive retrieval and reranking."
            ),
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "content-123"
    assert body["provider"] == "fake"
    assert body["language"] == "en"
    assert len(body["key_points"]) >= 1
    assert len(body["tags"]) >= 1


def test_create_summary_accepts_long_text_for_internal_compression() -> None:
    response = client.post(
        "/api/v1/summaries",
        json={
            "content_id": "long-content-123",
            "text": " ".join(["long article paragraph"] * 4000),
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "long-content-123"
