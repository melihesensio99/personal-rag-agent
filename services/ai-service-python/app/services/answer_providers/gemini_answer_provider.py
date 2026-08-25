from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.answers import AnswerRequest, AnswerResponse
from app.services.answer_providers.base import AnswerProvider
from app.services.prompt_loader import PromptLoader


class GeminiAnswerProvider(AnswerProvider):
    def __init__(
        self,
        prompt_loader: PromptLoader,
        api_key: str,
        model: str,
        base_url: str,
        timeout_seconds: int,
    ) -> None:
        self._prompt_loader = prompt_loader
        self._api_key = api_key
        self._model = model
        self._base_url = base_url.rstrip("/")
        self._timeout_seconds = timeout_seconds

    def create_answer(self, request: AnswerRequest) -> AnswerResponse:
        prepared_chunks = self._build_prepared_chunks(request)
        payload = self._send_request(" ".join(request.question.split()), prepared_chunks)
        output_text = self._extract_output_text(payload)

        if not isinstance(output_text, str) or not output_text.strip():
            raise ValueError("Gemini response did not include output_text.")

        parsed = self._parse_json_output(output_text)

        return AnswerResponse(
            content_id=request.content_id,
            answer=self._read_text(parsed, "answer", fallback="Yeterli bilgi bulunamadı."),
            used_chunk_indexes=self._read_int_list(parsed, "used_chunk_indexes"),
            language=self._read_text(parsed, "language", fallback="tr"),
            provider="gemini",
        )

    def _send_request(self, question: str, prepared_chunks: str) -> dict[str, object]:
        endpoint = f"{self._base_url}/models/{self._model}:generateContent"

        body = {
            "contents": [
                {
                    "role": "user",
                    "parts": [
                        {
                            "text": (
                                f"{self._prompt_loader.load()}\n\n"
                                "You answer user questions using only the provided retrieved chunks. "
                                "Always answer in Turkish. "
                                "Return only a JSON object with these exact fields: answer, used_chunk_indexes, language. "
                                "answer must be a short natural Turkish response grounded in the provided chunks. "
                                "used_chunk_indexes must be an array of integers from the provided chunk list. "
                                "language must be tr. "
                                "If the chunks do not contain enough information, say so clearly and return an empty used_chunk_indexes array.\n\n"
                                f"Question:\n{question}\n\nRetrieved chunks:\n{prepared_chunks}"
                            )
                        }
                    ],
                }
            ],
            "generationConfig": {
                "temperature": 0.2,
                "responseMimeType": "application/json",
            },
        }

        request = Request(
            endpoint,
            data=json.dumps(body).encode("utf-8"),
            headers={
                "Content-Type": "application/json",
                "x-goog-api-key": self._api_key,
            },
            method="POST",
        )

        try:
            with urlopen(request, timeout=self._timeout_seconds) as response:
                raw = response.read().decode("utf-8", errors="ignore")
        except HTTPError as exception:
            details = exception.read().decode("utf-8", errors="ignore")
            raise RuntimeError(f"Gemini answer request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Gemini answer request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Gemini answer request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Gemini answer response payload was invalid.")

        return parsed

    @staticmethod
    def _build_prepared_chunks(request: AnswerRequest) -> str:
        lines: list[str] = []

        for chunk in request.chunks:
            lines.append(
                "\n".join(
                    [
                        f"Index: {chunk.index}",
                        f"Content title: {chunk.content_title}",
                        f"Content URL: {chunk.content_url}",
                        f"Source type: {chunk.source_type}",
                        f"Content kind: {chunk.content_kind}",
                        f"Chunk index: {chunk.chunk_index}",
                        f"Distance: {chunk.distance}",
                        f"Similarity: {chunk.similarity}",
                        f"Text: {chunk.text}",
                    ]
                )
            )

        return "\n\n---\n\n".join(lines) if lines else "[NO CHUNKS]"

    @staticmethod
    def _extract_output_text(payload: dict[str, object]) -> str | None:
        candidates = payload.get("candidates")
        if not isinstance(candidates, list):
            return None

        for candidate in candidates:
            if not isinstance(candidate, dict):
                continue

            content = candidate.get("content")
            if not isinstance(content, dict):
                continue

            parts = content.get("parts")
            if not isinstance(parts, list):
                continue

            for part in parts:
                if isinstance(part, dict) and isinstance(part.get("text"), str):
                    return part["text"]

        return None

    def _parse_json_output(self, output_text: str) -> dict[str, object]:
        clean_text = output_text.strip()

        if clean_text.startswith("```"):
            clean_text = clean_text.removeprefix("```json").removeprefix("```").strip()
            clean_text = clean_text.removesuffix("```").strip()

        parsed = json.loads(clean_text)
        if not isinstance(parsed, dict):
            raise ValueError("Gemini answer JSON output was invalid.")

        return parsed

    def _read_text(self, parsed: dict[str, object], key: str, fallback: str) -> str:
        value = parsed.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()

        return fallback.strip()

    @staticmethod
    def _read_int_list(parsed: dict[str, object], key: str) -> list[int]:
        value = parsed.get(key)
        if not isinstance(value, list):
            return []

        return [item for item in value if isinstance(item, int)]
