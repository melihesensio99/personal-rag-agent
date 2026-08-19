from fastapi.testclient import TestClient

from app.contracts.summaries import SummaryRequest
from app.dependencies import get_summary_service
from app.main import app
from app.services.prompt_loader import PromptLoader
from app.services.summary_providers.base import SummaryProvider
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


class FailingSummaryProvider(SummaryProvider):
    def create_summary(self, request: SummaryRequest):
        raise TimeoutError("simulated gemini timeout")


def test_summary_service_falls_back_when_primary_provider_fails(tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("You are a summarizer.", encoding="utf-8")

    fallback_provider = FakeSummaryProvider(PromptLoader(str(prompt_file)))
    service = SummaryService(FailingSummaryProvider(), fallback_provider)

    response = service.create_summary(
        SummaryRequest(
            content_id="fallback-1",
            text="Bu bir test metnidir. Sistem hata alirsa fallback devreye girmelidir.",
        )
    )

    assert response.content_id == "fallback-1"
    assert response.provider == "fake"
    assert response.language == "tr"
    assert len(response.key_points) >= 1
