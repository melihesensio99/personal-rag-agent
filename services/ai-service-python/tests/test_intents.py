from fastapi.testclient import TestClient
import pytest

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
    assert body["action"] == "list_contents"
    assert body["intent"] == "search"
    assert body["query"] == "bugün attığım linkleri listele"
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
    assert body["action"] == "save_content"
    assert body["intent"] == "save"
    assert body["content"] == "RAG ile semantic search farkını öğrenmek istiyorum."


def test_mistral_intent_provider_parses_structured_output(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(
        self: MistralIntentProvider,
        request: IntentRequest,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"action":"list_contents","intent":"search","content_kind":"video",'
                            '"source_type":null,"time_filter":"today","keywords":["spor"],'
                            '"needs_clarification":false,"clarification_message":null}'
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
    assert result.action == "list_contents"
    assert result.query == "bugün attığım sporla ilgili videoları listele"
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

    attempts = {"count": 0}

    def fake_send_request(
        self: MistralIntentProvider,
        request: IntentRequest,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        attempts["count"] += 1

        if attempts["count"] == 1:
            return {
                "choices": [
                    {
                        "message": {
                            "content": (
                                '{"action":"answer","intent":"search","content_kind":"youtube",'
                                '"source_type":"any","time_filter":"bugün","keywords":"spor",'
                                '"needs_clarification":"false"}'
                            )
                        }
                    }
                ]
            }

        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"action":"list_contents","intent":"search","content_kind":"video",'
                            '"source_type":null,"time_filter":"today","keywords":["spor"],'
                            '"needs_clarification":false,"clarification_message":null}'
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
    assert result.action == "list_contents"
    assert result.content_kind == "video"
    assert result.source_type is None
    assert result.time_filter == "today"
    assert result.keywords == ["spor"]
    assert attempts["count"] == 2


def test_mistral_intent_provider_keeps_article_search_as_source_filter(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(
        self: MistralIntentProvider,
        request: IntentRequest,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"action":"list_contents","intent":"search","content_kind":null,'
                            '"source_type":"article","time_filter":"today","keywords":[],'
                            '"needs_clarification":false,"clarification_message":null}'
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
    assert result.action == "list_contents"
    assert result.content_kind is None
    assert result.source_type == "article"
    assert result.time_filter == "today"


def test_mistral_intent_provider_preserves_model_action_for_conceptual_questions(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(
        self: MistralIntentProvider,
        request: IntentRequest,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"action":"ask_clarification","intent":"clarify","content_kind":null,'
                            '"source_type":null,"time_filter":"none","keywords":["rag"],'
                            '"needs_clarification":true,"clarification_message":"Bunu arayayım mı?"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralIntentProvider, "_send_request", fake_send_request)

    message = (
        "Geniş ve karmaşık bir doküman kümesi üzerinde RAG kurarken, veriyi önceden "
        "indeksleme aşamasında mı derinleştirmeliyiz yoksa çıkarım anında modelin "
        "filtreleme ve arama yeteneklerine mi güvenmeliyiz?"
    )

    result = provider.classify(IntentRequest(message=message, current_date="2026-08-21"))

    assert result.action == "ask_clarification"
    assert result.intent == "clarify"
    assert result.query is None
    assert result.content is None
    assert result.needs_clarification is True


def test_mistral_intent_provider_raises_when_validation_cannot_be_repaired(monkeypatch) -> None:
    provider = MistralIntentProvider(
        api_key="test-key",
        model="ministral-3b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(
        self: MistralIntentProvider,
        request: IntentRequest,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"action":"answer","intent":"search","content_kind":"video",'
                            '"source_type":"any","time_filter":"bugün","keywords":"spor",'
                            '"needs_clarification":"false"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralIntentProvider, "_send_request", fake_send_request)

    with pytest.raises(RuntimeError, match="Mistral intent failed after retries"):
        provider.classify(
            IntentRequest(
                message="bugün attığım spor videolarını listele",
                current_date="2026-08-20",
            )
        )
