import json

from app.contracts.summaries import SummaryRequest
from app.services.prompt_loader import PromptLoader
from app.services.summary_providers.gemini_summary_provider import GeminiSummaryProvider


def test_gemini_summary_provider_parses_structured_output(monkeypatch, tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("You are a summarizer.", encoding="utf-8")

    provider = GeminiSummaryProvider(
        prompt_loader=PromptLoader(str(prompt_file)),
        api_key="test-key",
        model="gemini-3.6-flash",
        base_url="https://generativelanguage.googleapis.com/v1beta",
        timeout_seconds=30,
    )

    def fake_send_request(self: GeminiSummaryProvider, normalized_text: str) -> dict[str, object]:
        assert "retrieval augmented generation" in normalized_text.lower()
        return {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": json.dumps(
                                    {
                                        "title": "RAG explained",
                                        "short_summary": "This content explains retrieval augmented generation.",
                                        "key_points": [
                                            "RAG combines retrieval and generation.",
                                            "Chunking quality affects retrieval.",
                                        ],
                                        "tags": ["rag", "retrieval", "generation"],
                                        "language": "en",
                                    }
                                )
                            }
                        ]
                    }
                }
            ]
        }

    monkeypatch.setattr(GeminiSummaryProvider, "_send_request", fake_send_request)

    response = provider.create_summary(
        SummaryRequest(
            content_id="content-gemini-1",
            text="This article explains retrieval augmented generation and chunking quality.",
        )
    )

    assert response.content_id == "content-gemini-1"
    assert response.title == "RAG explained"
    assert response.short_summary.startswith("This content explains")
    assert response.key_points[0] == "RAG combines retrieval and generation."
    assert response.tags == ["rag", "retrieval", "generation"]
    assert response.language == "en"
    assert response.provider == "gemini"


def test_gemini_summary_provider_handles_missing_optional_fields(monkeypatch, tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("You are a summarizer.", encoding="utf-8")

    provider = GeminiSummaryProvider(
        prompt_loader=PromptLoader(str(prompt_file)),
        api_key="test-key",
        model="gemini-3.5-flash-lite",
        base_url="https://generativelanguage.googleapis.com/v1beta",
        timeout_seconds=30,
    )

    def fake_send_request(self: GeminiSummaryProvider, normalized_text: str) -> dict[str, object]:
        return {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": json.dumps(
                                    {
                                        "title": "Eksik alan testi",
                                        "short_summary": "Gemini bazen tags alanini eksik dondurebilir.",
                                        "key_points": ["Parser patlamadan fallback kullanmalidir."],
                                    }
                                )
                            }
                        ]
                    }
                }
            ]
        }

    monkeypatch.setattr(GeminiSummaryProvider, "_send_request", fake_send_request)

    response = provider.create_summary(
        SummaryRequest(
            content_id="content-gemini-missing-tags",
            text="Gemini bazen tags alanini eksik dondurebilir ve servis buna hazir olmalidir.",
        )
    )

    assert response.content_id == "content-gemini-missing-tags"
    assert response.title == "Eksik alan testi"
    assert response.tags
    assert response.language == "tr"
    assert response.provider == "gemini"
