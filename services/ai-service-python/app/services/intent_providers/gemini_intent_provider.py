from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


class GeminiIntentProvider(IntentProvider):
    def __init__(
        self,
        api_key: str,
        model: str,
        base_url: str,
        timeout_seconds: int,
    ) -> None:
        self._api_key = api_key
        self._model = model
        self._base_url = base_url.rstrip("/")
        self._timeout_seconds = timeout_seconds

    def classify(self, request: IntentRequest) -> IntentResponse:
        payload = self._send_request(request)
        output_text = self._extract_output_text(payload)

        if not isinstance(output_text, str) or not output_text.strip():
            raise ValueError("Gemini intent response did not include output_text.")

        parsed = json.loads(output_text)

        return IntentResponse(
            intent=parsed["intent"],
            content_kind=parsed.get("content_kind"),
            source_type=parsed.get("source_type"),
            time_filter=parsed.get("time_filter", "none"),
            keywords=parsed.get("keywords", []),
            needs_clarification=parsed.get("needs_clarification", False),
        )

    def _send_request(self, request: IntentRequest) -> dict[str, object]:
        endpoint = f"{self._base_url}/models/{self._model}:generateContent"

        prompt = (
            f"Today's date is {request.current_date}. "
            "Classify the user's message for a personal content assistant. "
            "Return only JSON. "
            "intent must be save, search, or clarify. "
            "content_kind must be text, video, image, or null. "
            "source_type must be article, youtube, pdf, image, telegram, or null. "
            "time_filter must be today, yesterday, two_days_ago, or none. "
            "keywords should contain only meaningful topic words and should not include filler words like getir, listele, attığım, linkleri. "
            "If the user asks for videos in general, set content_kind to video even when source_type is null. "
            "If the user asks for articles, writings, PDFs, or text-like records in general, set content_kind to text unless a stricter source_type is clearly requested. "
            "If the user asks for images, visuals, photos, or screenshots, set content_kind to image. "
            "If the user wants previously saved records, choose search. "
            "If the user sends content or a link to save, choose save.\n\n"
            f"User message: {request.message}"
        )

        body = {
            "contents": [
                {
                    "role": "user",
                    "parts": [{"text": prompt}],
                }
            ],
            "generationConfig": {
                "temperature": 0.1,
                "responseMimeType": "application/json",
            },
        }

        http_request = Request(
            endpoint,
            data=json.dumps(body).encode("utf-8"),
            headers={
                "Content-Type": "application/json",
                "x-goog-api-key": self._api_key,
            },
            method="POST",
        )

        try:
            with urlopen(http_request, timeout=self._timeout_seconds) as response:
                raw = response.read().decode("utf-8", errors="ignore")
        except HTTPError as exception:
            details = exception.read().decode("utf-8", errors="ignore")
            raise RuntimeError(f"Gemini intent request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Gemini intent request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Gemini intent request timed out.") from exception

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
