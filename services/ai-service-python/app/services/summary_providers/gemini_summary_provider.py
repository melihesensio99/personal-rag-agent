from __future__ import annotations

import json
from urllib.parse import urlencode
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

        parsed = json.loads(output_text)

        return SummaryResponse(
            content_id=request.content_id,
            title=parsed["title"].strip(),
            short_summary=parsed["short_summary"].strip(),
            key_points=[item.strip() for item in parsed["key_points"]],
            tags=[item.strip() for item in parsed["tags"]],
            language=parsed["language"].strip(),
            provider="gemini",
        )

    def _send_request(self, normalized_text: str) -> dict[str, object]:
        endpoint = f"{self._base_url}/interactions?{urlencode({'key': self._api_key})}"

        body = {
            "model": self._model,
            "input": (
                f"{self._prompt_loader.load()}\n\n"
                "Summarize the saved content below. "
                "Return only JSON matching the schema.\n\n"
                f"Content:\n{normalized_text}"
            ),
            "generation_config": {
                "thinking_level": "low",
                "temperature": 0.2,
            },
            "response_format": {
                "type": "text",
                "mime_type": "application/json",
                "schema": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "title": {"type": "string"},
                        "short_summary": {"type": "string"},
                        "key_points": {
                            "type": "array",
                            "items": {"type": "string"},
                            "minItems": 1,
                            "maxItems": 5,
                        },
                        "tags": {
                            "type": "array",
                            "items": {"type": "string"},
                            "minItems": 1,
                            "maxItems": 5,
                        },
                        "language": {"type": "string"},
                    },
                    "required": [
                        "title",
                        "short_summary",
                        "key_points",
                        "tags",
                        "language",
                    ],
                },
            },
        }

        request = Request(
            endpoint,
            data=json.dumps(body).encode("utf-8"),
            headers={"Content-Type": "application/json"},
            method="POST",
        )

        with urlopen(request, timeout=self._timeout_seconds) as response:
            raw = response.read().decode("utf-8", errors="ignore")

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Gemini response payload was invalid.")

        return parsed

    def _extract_output_text(self, payload: dict[str, object]) -> str | None:
        direct_output_text = payload.get("output_text")
        if isinstance(direct_output_text, str) and direct_output_text.strip():
            return direct_output_text

        steps = payload.get("steps")
        if not isinstance(steps, list):
            return None

        for step in steps:
            if not isinstance(step, dict):
                continue

            if step.get("type") != "model_output":
                continue

            content = step.get("content")
            if not isinstance(content, list):
                continue

            for item in content:
                if not isinstance(item, dict):
                    continue

                if item.get("type") == "text" and isinstance(item.get("text"), str):
                    return item["text"]

        return None

    def _normalize_whitespace(self, text: str) -> str:
        condensed = " ".join(text.split())
        if not condensed:
            raise ValueError("Summary input cannot be empty.")

        return condensed
