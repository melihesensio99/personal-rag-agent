from fastapi.testclient import TestClient

from app.dependencies import get_reranking_service
from app.main import app
from app.services.reranking_service import RerankingService


class FakeRerankModel:
    def predict(self, pairs: list[list[str]]) -> list[float]:
        assert pairs == [
            ["hangi belge daha alakali?", "Ilk belge RAG arama kalitesini anlatir."],
            ["hangi belge daha alakali?", "Ikinci belge alakasiz bir yemek tarifidir."],
        ]
        return [2.0, -2.0]


def _fake_reranking_service() -> RerankingService:
    service = RerankingService()
    service._get_model = lambda: FakeRerankModel()  # type: ignore[method-assign]
    return service


app.dependency_overrides[get_reranking_service] = _fake_reranking_service
client = TestClient(app)


def test_rerank_returns_scores_without_loading_real_model() -> None:
    response = client.post(
        "/api/v1/rerank",
        json={
            "query": "hangi belge daha alakali?",
            "documents": [
                {"index": 7, "text": "Ilk belge RAG arama kalitesini anlatir."},
                {"index": 3, "text": "Ikinci belge alakasiz bir yemek tarifidir."},
            ],
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["model"] == "BAAI/bge-reranker-v2-m3"
    assert body["scores"] == [
        {"index": 7, "score": 0.8807970779778823},
        {"index": 3, "score": 0.11920292202211755},
    ]


def test_rerank_rejects_empty_documents() -> None:
    response = client.post(
        "/api/v1/rerank",
        json={
            "query": "hangi belge daha alakali?",
            "documents": [],
        },
    )

    assert response.status_code == 422
