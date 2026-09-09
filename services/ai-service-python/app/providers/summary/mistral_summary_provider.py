from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.summaries import SummaryRequest, SummaryResponse
from app.utils.prompt_loader import PromptLoader
from app.services.summary_input_preparer import SummaryInputPreparer
from app.providers.summary.base import SummaryProvider
from app.utils.structured_output_runner import run_with_retries
from app.schemas.mistral_response_schemas import SUMMARY_SCHEMA, response_format


class MistralSummaryProvider(SummaryProvider):
    MAX_ATTEMPTS = 3
    SUMMARY_MAX_TOKENS = 3200

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

        def operation(repair_hint: str | None) -> SummaryResponse:
            payload = self._send_request(prepared_text, repair_hint)
            output_text = self._extract_output_text(payload)
            if not isinstance(output_text, str) or not output_text.strip():
                raise ValueError("Mistral summary response did not include output text.")

            parsed = self._parse_json_object(output_text)
            return SummaryResponse(
                content_id=request.content_id,
                title=self._read_text(parsed, "title"),
                short_summary=self._clean_markdown(
                    self._finish_at_sentence(self._read_text(parsed, "short_summary"))
                ),
                key_points=[
                    self._clean_markdown(self._finish_at_sentence(point))
                    for point in self._read_text_list(parsed, "key_points", minimum=1, maximum=3)
                ],
                tags=[self._clean_markdown(tag) for tag in self._read_text_list(parsed, "tags")],
                language=self._read_text(parsed, "language"),
                provider="mistral",
            )

        try:
            return run_with_retries(
                operation,
                self._build_repair_hint,
                max_attempts=self.MAX_ATTEMPTS,
                failure_message="Mistral summary failed after retries.",
                retryable_errors=(json.JSONDecodeError, RuntimeError, ValueError),
            )
        except RuntimeError:
            return self._build_fallback_summary(request, prepared_text)

    def _send_request(self, prepared_text: str, repair_hint: str | None = None) -> dict[str, object]:
        endpoint = f"{self._base_url}/chat/completions"

        system_prompt = (
            f"{self._prompt_loader.load()}\n\n"
            "You summarize saved personal content. "
            "Keep the response compact: short_summary must contain 3 bullet lines, and key_points must contain 3 concise items of 1-2 sentences each. "
            "Write short_summary, key_points and tags in Turkish, but preserve title exactly in the source language. "
            "Return only a JSON object with these exact fields: "
            "title, short_summary, key_points, tags, language. "
            "key_points and tags must be arrays of strings. "
            "language must be tr."
            " Before returning, proofread every Turkish field for spelling, accents and punctuation; use correct technical terms such as 'kafein', never phonetic typos such as 'kafetin'."
        )

        if repair_hint is not None:
            system_prompt += (
                " The previous output was invalid. Return only corrected JSON "
                "matching the required fields."
            )

        user_content = f"Content:\n{prepared_text}"
        if repair_hint is not None:
            user_content += (
                "\n\nPrevious output validation error:\n"
                f"{repair_hint}\nReturn only the corrected JSON object."
            )

        body = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_content},
            ],
            "temperature": 0.2,
            "max_tokens": self.SUMMARY_MAX_TOKENS,
            "response_format": response_format("summary_response", SUMMARY_SCHEMA),
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
    def _read_text_list(parsed: dict[str, object], key: str, minimum: int = 1, maximum: int | None = None) -> list[str]:
        value = parsed.get(key)
        if not isinstance(value, list):
            raise ValueError(f"Mistral summary JSON output is missing '{key}'.")

        items = [item.strip() for item in value if isinstance(item, str) and item.strip()]
        if len(items) < minimum:
            raise ValueError(f"Mistral summary JSON output must contain at least {minimum} '{key}'.")

        return items[:maximum] if maximum is not None else items

    @staticmethod
    def _finish_at_sentence(value: str) -> str:
        """Never expose a provider response that ends halfway through a sentence."""
        value = value.strip()
        last_stop = max(value.rfind("."), value.rfind("!"), value.rfind("?"))
        if last_stop >= 0 and last_stop >= len(value) // 2:
            return value[:last_stop + 1].strip()
        return value

    @staticmethod
    def _clean_markdown(value: str) -> str:
        """Keep provider formatting from leaking into plain-text UI fields."""
        return value.replace("**", "").replace("__", "").replace("`", "").strip()

    @staticmethod
    def _build_repair_hint(error: Exception) -> str:
        return (
            "Return a compact valid JSON object now. Do not explain anything outside JSON. "
            f"Previous error: {error}. "
            "Use exactly 3 short key_points, 3 short_summary bullet lines, 3-5 tags, and language 'tr'."
        )

    @classmethod
    def _build_fallback_summary(cls, request: SummaryRequest, prepared_text: str) -> SummaryResponse:
        """Create safe metadata without another model call when summary fails."""
        text = prepared_text.strip()
        lines = [line.strip() for line in text.splitlines() if line.strip()]
        title = next((line.lstrip("# ").strip() for line in lines if line.startswith("#")), "")
        if not title:
            title = cls._first_sentence(text) or "Kayıt özeti"

        sentences = [part.strip() for part in text.replace("\n", " ").split(".") if part.strip()]
        points = [cls._finish_at_sentence(sentence + ".") for sentence in sentences[:3]]
        while len(points) < 3:
            points.append("Kaynak metin, otomatik özet üretimi başarısız olduğu için ayrıntılı olarak işlenemedi.")

        return SummaryResponse(
            content_id=request.content_id,
            title=title[:500],
            short_summary=(
                "- Konu: Kaynak metinden otomatik özet üretilemedi.\n"
                "- Kapsam: İçerik kaydedildi; bu özet yalnızca güvenli bir yedek bilgidir.\n"
                "- Not: Ayrıntılı özet için daha sonra yeniden denenebilir."
            ),
            key_points=points,
            tags=[],
            language="tr",
            provider="mistral",
        )

    @staticmethod
    def _first_sentence(text: str) -> str:
        return next((part.strip() for part in text.replace("\n", " ").split(".") if part.strip()), "")
