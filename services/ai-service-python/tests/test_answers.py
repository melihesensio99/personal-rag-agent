import json

from fastapi.testclient import TestClient

from app.dependencies import get_answer_service
from app.main import app
from app.services.answer_providers.fake_answer_provider import FakeAnswerProvider
from app.services.answer_providers.mistral_answer_provider import MistralAnswerProvider
from app.services.answer_service import AnswerService
from app.services.prompt_loader import PromptLoader
from app.contracts.answers import AnswerContextChunk, AnswerRequest


def _fake_answer_service() -> AnswerService:
    prompt_loader = PromptLoader("app/prompts/content_answer_v1.txt")
    return AnswerService(FakeAnswerProvider(prompt_loader))


app.dependency_overrides[get_answer_service] = _fake_answer_service

client = TestClient(app)


def test_create_answer_returns_structured_response() -> None:
    response = client.post(
        "/api/v1/answers",
        json={
            "content_id": "answer-demo-1",
            "question": "Kas yapmak için günlük ne kadar protein almalıyım?",
            "chunks": [
                {
                    "index": 0,
                    "content_id": "content-1",
                    "chunk_id": "chunk-1",
                    "content_title": "Kas Hacmi Artırmak",
                    "content_url": "https://health.clevelandclinic.org/hypertrophy",
                    "source_type": "article",
                    "content_kind": "text",
                    "chunk_index": 0,
                    "text": "Protein alımının 1.6-2.2 gram/kg olması önerilir.",
                    "distance": 0.12,
                    "similarity": 0.88,
                }
            ],
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["content_id"] == "answer-demo-1"
    assert body["provider"] == "fake"
    assert body["language"] == "tr"
    assert len(body["answer"]) > 0
    assert body["used_chunk_indexes"] == [0]


def test_mistral_answer_provider_accepts_common_answer_field_aliases(monkeypatch) -> None:
    provider = MistralAnswerProvider(
        prompt_loader=PromptLoader("app/prompts/content_answer_v1.txt"),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralAnswerProvider, question: str, prepared_chunks: str, repair_hint: str | None = None) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"cevap":"Protein için kaynak 1.6-2.2 gram/kg aralığını öneriyor.",'
                            '"used_chunks":[0],"lang":"tr"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralAnswerProvider, "_send_request", fake_send_request)

    result = provider.create_answer(
        AnswerRequest(
            content_id="answer-demo-1",
            question="Kas yapmak için günlük ne kadar protein almalıyım?",
            chunks=[
                AnswerContextChunk(
                    index=0,
                    content_id="content-1",
                    chunk_id="chunk-1",
                    content_title="Kas Hacmi Artırmak",
                    content_url="https://health.clevelandclinic.org/hypertrophy",
                    source_type="article",
                    content_kind="text",
                    chunk_index=1,
                    text="Protein alımının 1.6-2.2 gram/kg olması önerilir.",
                    distance=0.12,
                    similarity=0.88,
                )
            ],
        )
    )

    assert result.answer == "Protein için kaynak 1.6-2.2 gram/kg aralığını öneriyor."
    assert result.used_chunk_indexes == [0]
    assert result.language == "tr"


def test_mistral_answer_provider_accepts_content_as_answer_alias(monkeypatch) -> None:
    provider = MistralAnswerProvider(
        prompt_loader=PromptLoader("app/prompts/content_answer_v1.txt"),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralAnswerProvider, question: str, prepared_chunks: str, repair_hint: str | None = None) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"content":"Protein için 1.6-2.2 gram/kg önerilir.",'
                            '"used_chunk_indexes":[0],"language":"tr"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralAnswerProvider, "_send_request", fake_send_request)

    result = provider.create_answer(
        AnswerRequest(
            content_id="answer-demo-1",
            question="Kas yapmak için günlük ne kadar protein almalıyım?",
            chunks=[
                AnswerContextChunk(
                    index=0,
                    content_id="content-1",
                    chunk_id="chunk-1",
                    content_title="Kas Hacmi Artırmak",
                    content_url="https://health.clevelandclinic.org/hypertrophy",
                    source_type="article",
                    content_kind="text",
                    chunk_index=1,
                    text="Protein alımının 1.6-2.2 gram/kg olması önerilir.",
                    distance=0.12,
                    similarity=0.88,
                )
            ],
        )
    )

    assert result.answer == "Protein için 1.6-2.2 gram/kg önerilir."
    assert result.used_chunk_indexes == [0]
    assert result.language == "tr"


def test_mistral_answer_provider_repairs_invalid_answer_shape(monkeypatch) -> None:
    provider = MistralAnswerProvider(
        prompt_loader=PromptLoader("app/prompts/content_answer_v1.txt"),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )
    attempts = {"count": 0, "repair_seen": False}

    def fake_send_request(
        self: MistralAnswerProvider,
        question: str,
        prepared_chunks: str,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        attempts["count"] += 1
        if repair_hint is not None:
            attempts["repair_seen"] = True

        answer = "invalid-shape" if attempts["count"] == 1 else "Düzeltilmiş cevap."
        if attempts["count"] == 1:
            answer_value: object = {"text": answer}
        else:
            answer_value = answer

        return {
            "choices": [
                {
                    "message": {
                        "content": json.dumps(
                            {
                                "answer": answer_value,
                                "used_chunk_indexes": [0],
                                "language": "tr",
                            }
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralAnswerProvider, "_send_request", fake_send_request)

    result = provider.create_answer(
        AnswerRequest(
            content_id="answer-demo-1",
            question="Mikroservis mi monolit mi?",
            chunks=[
                AnswerContextChunk(
                    index=0,
                    content_id="content-1",
                    chunk_id="chunk-1",
                    content_title="Mimari",
                    content_url="https://example.com/article",
                    source_type="article",
                    content_kind="text",
                    chunk_index=0,
                    text="İki yaklaşımın farklı avantajları vardır.",
                    distance=0.1,
                    similarity=0.9,
                )
            ],
        )
    )

    assert result.answer == "Düzeltilmiş cevap."
    assert attempts == {"count": 2, "repair_seen": True}
