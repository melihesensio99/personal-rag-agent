from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.answers import AnswerRequest, AnswerResponse
from app.services.answer_providers.base import AnswerProvider
from app.services.prompt_loader import PromptLoader
from app.services.structured_output_runner import run_with_retries
from app.services.mistral_response_schemas import ANSWER_SCHEMA, response_format


class MistralAnswerProvider(AnswerProvider):
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

    def create_answer(self, request: AnswerRequest) -> AnswerResponse:
        prepared_question = " ".join(request.question.split())
        prepared_chunks = self._build_prepared_chunks(request)

        def operation(repair_hint: str | None) -> AnswerResponse:
            payload = self._send_request(prepared_question, prepared_chunks, repair_hint)
            output_text = self._extract_output_text(payload)
            if not isinstance(output_text, str) or not output_text.strip():
                raise ValueError("Mistral answer response did not include output text.")

            parsed = self._parse_json_object(output_text)
            return AnswerResponse(
                content_id=request.content_id,
                answer=self._read_first_text(parsed, [
                    "answer", "cevap", "response", "final_answer", "result",
                    "content", "text", "message", "final", "answer_text", "result_text",
                ]),
                used_chunk_indexes=self._read_first_int_list(parsed, [
                    "used_chunk_indexes", "used_chunks", "chunk_indexes", "source_indexes",
                    "used_indexes", "indexes",
                ]),
                language=self._read_first_text(parsed, ["language", "lang"], fallback="tr"),
                provider="mistral",
            )

        return run_with_retries(
            operation,
            lambda error: self._build_repair_hint(error, prepared_question),
            max_attempts=self.MAX_ATTEMPTS,
            failure_message="Mistral answer failed after retries.",
            retryable_errors=(json.JSONDecodeError, RuntimeError, ValueError),
        )

    def _send_request(
        self,
        question: str,
        prepared_chunks: str,
        repair_hint: str | None = None,
    ) -> dict[str, object]:
        endpoint = f"{self._base_url}/chat/completions"

        system_prompt = self._prompt_loader.load()

        if repair_hint is not None:
            system_prompt += (
                "\nThe previous answer JSON was invalid. Return only corrected JSON "
                "matching the required schema.\n"
            )

        user_content = (
            f"Question:\n{question}\n\n"
            f"Retrieved chunks:\n{prepared_chunks}"
        )

        if repair_hint is not None:
            user_content += (
                "\n\nPrevious output validation error:\n"
                f"{repair_hint}\n"
                "Return only the corrected JSON object."
            )

        body = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_content},
            ],
            "temperature": 0.2,
            "response_format": response_format("answer_response", ANSWER_SCHEMA),
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
            raise RuntimeError(f"Mistral answer request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Mistral answer request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Mistral answer request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Mistral answer response payload was invalid.")

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
    def _build_repair_hint(error: Exception, question: str) -> str:
        return (
            f"Question: {question}\n"
            "The previous response did not match the required answer schema.\n"
            f"Error: {error}\n"
            "Use a non-empty string for answer, an integer array for "
            "used_chunk_indexes, and language='tr'."
        )

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
            raise ValueError("Mistral answer JSON output was not an object.")

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
    def _read_first_text(
        parsed: dict[str, object],
        keys: list[str],
        fallback: str = "",
    ) -> str:
        for key in keys:
            value = parsed.get(key)
            if isinstance(value, str) and value.strip():
                return value.strip()

        if fallback:
            return fallback

        raise ValueError(
            "Mistral answer JSON output is missing one of these fields: "
            f"{', '.join(keys)}. Available keys: {', '.join(sorted(parsed.keys())) or '[none]'}."
        )

    @staticmethod
    def _read_first_int_list(parsed: dict[str, object], keys: list[str]) -> list[int]:
        for key in keys:
            value = parsed.get(key)
            if not isinstance(value, list):
                continue

            return [item for item in value if isinstance(item, int)]

        raise ValueError(
            "Mistral answer JSON output is missing one of these fields: "
            f"{', '.join(keys)}. Available keys: {', '.join(sorted(parsed.keys())) or '[none]'}."
        )
