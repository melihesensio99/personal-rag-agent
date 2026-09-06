import importlib

from fastapi.testclient import TestClient

from app.main import app


def test_health_returns_typed_service_status() -> None:
    response = TestClient(app).get("/health")

    assert response.status_code == 200
    assert response.json() == {
        "service": "ai-service-python",
        "status": "healthy",
        "version": "1.0",
    }


def test_application_warms_reranker_before_becoming_healthy(monkeypatch) -> None:
    main_module = importlib.import_module("app.main")

    class FakeReranker:
        warmed_up = False

        def warm_up(self) -> None:
            self.warmed_up = True

    reranker = FakeReranker()
    monkeypatch.setattr(main_module, "get_reranking_service", lambda: reranker)

    with TestClient(app) as client:
        response = client.get("/health")

    assert response.status_code == 200
    assert reranker.warmed_up is True
