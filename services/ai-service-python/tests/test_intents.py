from fastapi.testclient import TestClient

from app.dependencies import get_intent_service
from app.main import app
from app.services.intent_providers.fake_intent_provider import FakeIntentProvider
from app.services.intent_service import IntentService


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
