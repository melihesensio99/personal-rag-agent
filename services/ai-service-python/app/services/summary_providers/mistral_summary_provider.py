from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.services.prompt_loader import PromptLoader
from app.services.summary_input_preparer import SummaryInputPreparer
from app.services.summary_providers.base import SummaryProvider


class MistralSummaryProvider(SummaryProvider):
    MAX_ATTEMPTS = 3

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
        prepared_text = SummaryInputPreparer.prepare(request.text)
        last_error: Exception | None = None

        for _ in range(self.MAX_ATTEMPTS):
            try:
                payload = self._send_request(prepared_text)
                output_text = self._extract_output_text(payload)

                if not isinstance(output_text, str) or not output_text.strip():
                    raise ValueError("Mistral summary response did not include output text.")

                parsed = self._parse_json_object(output_text)

                return SummaryResponse(
                    content_id=request.content_id,
                    title=self._read_text(parsed, "title"),
                    short_summary=self._read_text(parsed, "short_summary"),
                    key_points=self._read_text_list(parsed, "key_points"),
                    tags=self._read_text_list(parsed, "tags"),
                    language=self._read_text(parsed, "language"),
                    provider="mistral",
                )
            except (json.JSONDecodeError, RuntimeError, ValueError) as error:
                last_error = error

        raise RuntimeError("Mistral summary failed after retries.") from last_error

    def _send_request(self, prepared_text: str) -> dict[str, object]:
        endpoint = f"{self._base_url}/chat/completions"

        system_prompt = (
            f"{self._prompt_loader.load()}\n\n"
            "You summarize saved personal content. "
            "Always answer in Turkish. "
            "Return only a JSON object with these exact fields: "
            "title, short_summary, key_points, tags, language. "
            "key_points and tags must be arrays of strings. "
            "language must be tr."
        )

        body = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": f"Content:\n{prepared_text}"},
            ],
            "temperature": 0.2,
            "response_format": {"type": "json_object"},
        }

        http_request = Request(
            endpoint,
            data=json.dumps(body).encode("utf-8"),
            headers={
                "Authorization": f"Bearer {self._api_key}",
                "Content-Type": "application/json",
            },
            method="POST",
        )

        try:
            with urlopen(http_request, timeout=self._timeout_seconds) as response:
                raw = response.read().decode("utf-8", errors="ignore")
        except HTTPError as exception:
            details = exception.read().decode("utf-8", errors="ignore")
            raise RuntimeError(f"Mistral summary request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Mistral summary request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Mistral summary request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Mistral summary response payload was invalid.")

        return parsed

    @staticmethod
    def _extract_output_text(payload: dict[str, object]) -> str | None:
        choices = payload.get("choices")
        if not isinstance(choices, list):
            return None

        for choice in choices:
            if not isinstance(choice, dict):
                continue

            message = choice.get("message")
            if not isinstance(message, dict):
                continue

            content = message.get("content")
            if isinstance(content, str):
                return content

        return None

    @classmethod
    def _parse_json_object(cls, output_text: str) -> dict[str, object]:
        try:
            parsed = json.loads(output_text)
        except json.JSONDecodeError:
            parsed = json.loads(cls._extract_first_json_object(output_text))

        if not isinstance(parsed, dict):
            raise ValueError("Mistral summary JSON output was not an object.")

        return parsed

    @staticmethod
    def _extract_first_json_object(output_text: str) -> str:
        start = output_text.find("{")
        if start < 0:
            raise json.JSONDecodeError("No JSON object start found", output_text, 0)

        depth = 0
        in_string = False
        escaped = False

        for index in range(start, len(output_text)):
            char = output_text[index]

            if escaped:
                escaped = False
                continue

            if char == "\\" and in_string:
                escaped = True
                continue

            if char == '"':
                in_string = not in_string
                continue

            if in_string:
                continue

            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    return output_text[start : index + 1]

        raise json.JSONDecodeError("No complete JSON object found", output_text, start)

    @staticmethod
    def _read_text(parsed: dict[str, object], key: str) -> str:
        value = parsed.get(key)
        if not isinstance(value, str) or not value.strip():
            raise ValueError(f"Mistral summary JSON output is missing '{key}'.")

        return value.strip()

    @staticmethod
    def _read_text_list(parsed: dict[str, object], key: str) -> list[str]:
        value = parsed.get(key)
        if not isinstance(value, list):
            raise ValueError(f"Mistral summary JSON output is missing '{key}'.")

        items = [item.strip() for item in value if isinstance(item, str) and item.strip()]
        if not items:
            raise ValueError(f"Mistral summary JSON output has an empty '{key}'.")

        return items
