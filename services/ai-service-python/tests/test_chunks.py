from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_create_chunks_uses_overlap() -> None:
    response = client.post(
        "/api/v1/chunks",
        json={
            "content_id": "chunk-demo-1",
            "text": "ABCDEFGHIJ",
            "chunk_size": 4,
            "overlap": 1,
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "chunk-demo-1"
    assert body["chunk_size"] == 4
    assert body["overlap"] == 1
    assert body["total_chunks"] == 3
    assert [chunk["text"] for chunk in body["chunks"]] == ["ABCD", "DEFG", "GHIJ"]
    assert body["chunks"][1]["char_start"] == 3


def test_create_chunks_rejects_overlap_equal_to_chunk_size() -> None:
    response = client.post(
        "/api/v1/chunks",
        json={
            "content_id": "chunk-demo-invalid",
            "text": "ABCDEFGHIJ",
            "chunk_size": 400,
            "overlap": 400,
        },
    )

    assert response.status_code == 422


def test_create_chunks_normalizes_whitespace() -> None:
    response = client.post(
        "/api/v1/chunks",
        json={
            "content_id": "chunk-demo-whitespace",
            "text": "RAG   uses\n\nretrieval.   LLM uses context.",
            "chunk_size": 200,
            "overlap": 0,
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["total_chunks"] == 1
    assert body["chunks"][0]["text"] == "RAG uses retrieval. LLM uses context."


def test_create_chunks_accepts_long_extracted_text() -> None:
    response = client.post(
        "/api/v1/chunks",
        json={
            "content_id": "chunk-demo-long",
            "text": " ".join(["long article paragraph"] * 4000),
            "chunk_size": 1200,
            "overlap": 150,
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "chunk-demo-long"
    assert body["total_chunks"] > 1
