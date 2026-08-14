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

