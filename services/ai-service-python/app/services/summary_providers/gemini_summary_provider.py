from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.prompt_loader import PromptLoader
from app.services.summary_providers.base import SummaryProvider


class GeminiSummaryProvider(SummaryProvider):
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

    def create_summary(self, request: SummaryRequest) -> SummaryResponse:
        normalized_text = self._normalize_whitespace(request.text)
        payload = self._send_request(normalized_text)
        output_text = self._extract_output_text(payload)

        if not isinstance(output_text, str) or not output_text.strip():
            raise ValueError("Gemini response did not include output_text.")

        parsed = self._parse_json_output(output_text)

        return SummaryResponse(
            content_id=request.content_id,
            title=self._read_text(parsed, "title", fallback="Basliksiz icerik"),
            short_summary=self._read_text(parsed, "short_summary", fallback=normalized_text[:280]),
            key_points=self._read_text_list(parsed, "key_points", fallback=[normalized_text[:140]]),
            tags=self._read_text_list(parsed, "tags", fallback=self._build_fallback_tags(normalized_text)),
            language=self._read_text(parsed, "language", fallback="tr"),
            provider="gemini",
        )

    def _send_request(self, normalized_text: str) -> dict[str, object]:
        endpoint = f"{self._base_url}/models/{self._model}:generateContent"

        body = {
            "contents": [
                {
                    "role": "user",
                    "parts": [
                        {
                            "text": (
                                f"{self._prompt_loader.load()}\n\n"
                                "Summarize the saved content below. "
                                "Return only a JSON object with these exact fields: "
                                "title, short_summary, key_points, tags, language. "
                                "key_points and tags must be arrays of strings. "
                                "language must be tr.\n\n"
                                f"Content:\n{normalized_text}"
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
            raise RuntimeError(f"Gemini summary request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Gemini summary request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Gemini summary request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Gemini response payload was invalid.")

        return parsed

    def _extract_output_text(self, payload: dict[str, object]) -> str | None:
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
            raise ValueError("Gemini summary JSON output was invalid.")

        return parsed

    def _read_text(self, parsed: dict[str, object], key: str, fallback: str) -> str:
        value = parsed.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()

        return fallback.strip()

    def _read_text_list(self, parsed: dict[str, object], key: str, fallback: list[str]) -> list[str]:
        value = parsed.get(key)
        if isinstance(value, list):
            items = [item.strip() for item in value if isinstance(item, str) and item.strip()]
            if items:
                return items

        return fallback

    def _build_fallback_tags(self, text: str) -> list[str]:
        words = [
            word.strip(".,:;!?()[]{}\"'").lower()
            for word in text.split()
            if len(word.strip(".,:;!?()[]{}\"'")) >= 4
        ]
        unique_words: list[str] = []

        for word in words:
            if word in unique_words:
                continue

            unique_words.append(word)

            if len(unique_words) == 4:
                break

        return unique_words or ["genel"]

    def _normalize_whitespace(self, text: str) -> str:
        condensed = " ".join(text.split())
        if not condensed:
            raise ValueError("Summary input cannot be empty.")

        return condensed
