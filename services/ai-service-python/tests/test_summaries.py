from fastapi.testclient import TestClient

from app.main import app

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
