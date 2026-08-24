from fastapi.testclient import TestClient

from app.dependencies import get_embedding_service
from app.main import app
from app.services.embedding_providers.fake_embedding_provider import FakeEmbeddingProvider
from app.services.embedding_service import EmbeddingService

app.dependency_overrides[get_embedding_service] = lambda: EmbeddingService(
    FakeEmbeddingProvider(dimension=1024),
    expected_dimension=1024,
)
client = TestClient(app)


def test_create_embeddings_returns_one_vector_per_text() -> None:
    response = client.post(
        "/api/v1/embeddings",
        json={
            "content_id": "embedding-demo-1",
            "texts": [
                "RAG uses retrieval to find relevant chunks.",
                "Embeddings represent text as vectors.",
            ],
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "embedding-demo-1"
    assert body["model"] == "fake"
    assert body["dimension"] == 1024
    assert len(body["embeddings"]) == 2
    assert body["embeddings"][0]["index"] == 0
    assert len(body["embeddings"][0]["embedding"]) == 1024


def test_create_embeddings_rejects_empty_text_items() -> None:
    response = client.post(
        "/api/v1/embeddings",
        json={
            "content_id": "embedding-demo-invalid",
            "texts": ["valid text", " "],
        },
    )

    assert response.status_code == 422
