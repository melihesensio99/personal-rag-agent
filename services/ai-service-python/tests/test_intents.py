from fastapi.testclient import TestClient

from app.dependencies import get_intent_service
from app.main import app
from app.services.intent_providers.fake_intent_provider import FakeIntentProvider
from app.services.intent_providers.mistral_intent_provider import MistralIntentProvider
from app.services.intent_service import IntentService
from app.contracts.intents import IntentRequest


def _fake_intent_service() -> IntentService:
    return IntentService(FakeIntentProvider())


app.dependency_overrides[get_intent_service] = _fake_intent_service

client = TestClient(app)


def test_search_intent_for_today_links() -> None:
    response = client.post(
        "/api/v1/intents",
        json={
            "message": "bugün attığım linkleri listele",
            "current_date": "2026-08-16",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["intent"] == "search"
    assert body["time_filter"] == "today"


def test_save_intent_for_regular_content() -> None:
    response = client.post(
        "/api/v1/intents",
        json={
            "message": "RAG ile semantic search farkını öğrenmek istiyorum.",
            "current_date": "2026-08-16",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["intent"] == "save"


def test_mistral_intent_provider_parses_structured_output(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralIntentProvider, request: IntentRequest) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"intent":"search","content_kind":"video","source_type":null,'
                            '"time_filter":"today","keywords":["spor"],"needs_clarification":false}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralIntentProvider, "_send_request", fake_send_request)

    result = provider.classify(
        IntentRequest(
            message="bugün attığım sporla ilgili videoları listele",
            current_date="2026-08-20",
        )
    )

    assert result.intent == "search"
    assert result.content_kind == "video"
    assert result.time_filter == "today"
    assert result.keywords == ["spor"]


def test_mistral_intent_provider_normalizes_non_contract_values(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralIntentProvider, request: IntentRequest) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"intent":"retrieve","content_kind":"youtube","source_type":"any",'
                            '"time_filter":"bugün","keywords":["bugün","spor","listele"],'
                            '"needs_clarification":"false"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralIntentProvider, "_send_request", fake_send_request)

    result = provider.classify(
        IntentRequest(
            message="bugün attığım spor videolarını listele",
            current_date="2026-08-20",
        )
    )

    assert result.intent == "search"
    assert result.content_kind == "video"
    assert result.source_type is None
    assert result.time_filter == "today"
    assert result.keywords == ["spor"]


def test_mistral_intent_provider_keeps_article_search_as_source_filter(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralIntentProvider, request: IntentRequest) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"intent":"search","content_kind":null,"source_type":"article",'
                            '"time_filter":"today","keywords":[],"needs_clarification":false}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralIntentProvider, "_send_request", fake_send_request)

    result = provider.classify(
        IntentRequest(
            message="bugün attığım makaleleri getir",
            current_date="2026-08-21",
        )
    )

    assert result.intent == "search"
    assert result.content_kind is None
    assert result.source_type == "article"
    assert result.time_filter == "today"
