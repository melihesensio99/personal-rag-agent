from app.contracts.summaries import SummaryRequest
from app.services.prompt_loader import PromptLoader
from app.services.summary_providers.mistral_summary_provider import MistralSummaryProvider


def test_mistral_summary_provider_parses_structured_output(monkeypatch, tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("Summarize content.", encoding="utf-8")

    provider = MistralSummaryProvider(
        prompt_loader=PromptLoader(str(prompt_file)),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralSummaryProvider, normalized_text: str) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"title":"RAG nedir?","short_summary":"RAG, ilgili bilgileri getirip LLM ile cevap üretir.",'
                            '"key_points":["Retrieval bilgi bulur.","Generation cevap üretir."],'
                            '"tags":["rag","llm"],"language":"tr"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralSummaryProvider, "_send_request", fake_send_request)

    response = provider.create_summary(
        SummaryRequest(
            content_id="mistral-summary-1",
            text="RAG, retrieval ve generation adımlarını birleştirir.",
        )
    )

    assert response.provider == "mistral"
    assert response.title == "RAG nedir?"
    assert response.language == "tr"
    assert response.tags == ["rag", "llm"]


def test_mistral_summary_provider_extracts_json_from_wrapped_output(monkeypatch, tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("Summarize content.", encoding="utf-8")

    provider = MistralSummaryProvider(
        prompt_loader=PromptLoader(str(prompt_file)),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )

    def fake_send_request(self: MistralSummaryProvider, normalized_text: str) -> dict[str, object]:
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            "Elbette, JSON aşağıda:\n"
                            '{"title":"Chunking","short_summary":"Metin parçalara ayrılır.",'
                            '"key_points":["Uzun metin bölünür."],'
                            '"tags":["chunking"],"language":"tr"}'
                            "\nUmarım yardımcı olur."
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralSummaryProvider, "_send_request", fake_send_request)

    response = provider.create_summary(
        SummaryRequest(
            content_id="mistral-summary-wrapped-1",
            text="Chunking uzun metni yönetilebilir parçalara böler.",
        )
    )

    assert response.title == "Chunking"
    assert response.key_points == ["Uzun metin bölünür."]


def test_mistral_summary_provider_compresses_long_input(monkeypatch, tmp_path) -> None:
    prompt_file = tmp_path / "prompt.txt"
    prompt_file.write_text("Summarize content.", encoding="utf-8")

    provider = MistralSummaryProvider(
        prompt_loader=PromptLoader(str(prompt_file)),
        api_key="test-key",
        model="ministral-8b-2512",
        base_url="https://api.mistral.ai/v1",
        timeout_seconds=5,
    )
    captured_input: dict[str, str] = {}

    def fake_send_request(self: MistralSummaryProvider, normalized_text: str) -> dict[str, object]:
        captured_input["text"] = normalized_text
        return {
            "choices": [
                {
                    "message": {
                        "content": (
                            '{"title":"Uzun içerik","short_summary":"Uzun içerik sıkıştırılarak özetlendi.",'
                            '"key_points":["Baş, orta ve son bölümler kullanıldı."],'
                            '"tags":["uzun içerik"],"language":"tr"}'
                        )
                    }
                }
            ]
        }

    monkeypatch.setattr(MistralSummaryProvider, "_send_request", fake_send_request)

    response = provider.create_summary(
        SummaryRequest(
            content_id="long-summary-1",
            text=" ".join(["wikipedia"] * 25000),
        )
    )

    assert response.title == "Uzun içerik"
    assert "[CONTENT COMPRESSED FOR SUMMARY]" in captured_input["text"]
    assert len(captured_input["text"]) < 19000
