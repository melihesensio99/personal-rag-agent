from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.intents import IntentRequest, IntentResponse
from app.providers.intent.base import IntentProvider
from app.utils.prompt_loader import PromptLoader


class GeminiIntentProvider(IntentProvider):
    _ACTION_ALIASES = {
        "save_content": "save_content",
        "save": "save_content",
        "store": "save_content",
        "kaydet": "save_content",
        "list_contents": "list_contents",
        "list": "list_contents",
        "retrieve": "list_contents",
        "get": "list_contents",
        "search": "list_contents",
        "find": "list_contents",
        "listele": "list_contents",
        "getir": "list_contents",
        "bul": "list_contents",
        "answer_from_memory": "answer_from_memory",
        "answer": "answer_from_memory",
        "question": "answer_from_memory",
        "qa": "answer_from_memory",
        "rag_answer": "answer_from_memory",
        "ask_clarification": "ask_clarification",
        "clarify": "ask_clarification",
        "unclear": "ask_clarification",
        "belirsiz": "ask_clarification",
    }

    def __init__(
        self,
        api_key: str,
        model: str,
        base_url: str,
        timeout_seconds: int,
        prompt_loader: PromptLoader | None = None,
    ) -> None:
        self._api_key = api_key
        self._model = model
        self._base_url = base_url.rstrip("/")
        self._timeout_seconds = timeout_seconds
        self._prompt_loader = prompt_loader or PromptLoader("app/prompts/content_intent_v1.txt")

    def classify(self, request: IntentRequest) -> IntentResponse:
        payload = self._send_request(request)
        output_text = self._extract_output_text(payload)

        if not isinstance(output_text, str) or not output_text.strip():
            raise ValueError("Gemini intent response did not include output_text.")

        parsed = json.loads(output_text)
        action = self._normalize_action(parsed.get("action"), parsed.get("intent"), request.message)
        query = self._normalize_optional_text(parsed.get("query"))
        content = self._normalize_optional_text(parsed.get("content"))

        response = IntentResponse(
            action=action,
            intent=self._derive_intent_from_action(action),
            query=(query or request.message) if action in {"list_contents", "answer_from_memory"} else None,
            content=(content or request.message) if action == "save_content" else None,
            content_kind=parsed.get("content_kind"),
            source_type=parsed.get("source_type"),
            time_filter=parsed.get("time_filter", "none"),
            date_from=self._normalize_date(parsed.get("date_from")),
            date_to=self._normalize_date(parsed.get("date_to")),
            semantic_query=(
                self._normalize_optional_text(parsed.get("semantic_query"))
                if action == "answer_from_memory"
                else None
            ),
            keywords=parsed.get("keywords", []),
            needs_clarification=parsed.get("needs_clarification", False),
            clarification_message=self._normalize_optional_text(parsed.get("clarification_message")),
        )

        if response.action != "save_content" and self._looks_like_content_to_save(request.message):
            return response.model_copy(
                update={
                    "action": "save_content",
                    "intent": "save",
                    "query": None,
                    "content": request.message,
                    "content_kind": "text",
                    "source_type": "telegram",
                    "time_filter": "none",
                    "date_from": None,
                    "date_to": None,
                    "semantic_query": None,
                    "keywords": [],
                    "needs_clarification": False,
                }
            )

        if response.action in {"save_content", "ask_clarification"} and self._looks_like_answer_question(request.message):
            return response.model_copy(
                update={
                    "action": "answer_from_memory",
                    "intent": "search",
                    "query": request.message,
                    "content": None,
                    "content_kind": None,
                    "source_type": None,
                    "time_filter": "none",
                    "date_from": None,
                    "date_to": None,
                    "semantic_query": None,
                    "needs_clarification": False,
                    "clarification_message": None,
                }
            )

        return response

    def _send_request(self, request: IntentRequest) -> dict[str, object]:
        endpoint = f"{self._base_url}/models/{self._model}:generateContent"

        prompt = (
            f"Today's date is {request.current_date}.\n\n"
            f"{self._prompt_loader.load()}\n\n"
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

    @classmethod
    def _normalize_action(cls, action_value: object, intent_value: object, message: str) -> str:
        if isinstance(action_value, str):
            normalized = action_value.strip().lower().replace(" ", "_")
            if normalized in cls._ACTION_ALIASES:
                return cls._ACTION_ALIASES[normalized]

        intent = str(intent_value).strip().lower() if intent_value is not None else "clarify"
        if intent == "save":
            return "save_content"

        if intent == "clarify":
            return "ask_clarification"

        return "answer_from_memory" if cls._looks_like_answer_question(message) else "list_contents"

    @staticmethod
    def _derive_intent_from_action(action: str) -> str:
        if action == "save_content":
            return "save"

        if action == "ask_clarification":
            return "clarify"

        return "search"

    @staticmethod
    def _normalize_optional_text(value: object) -> str | None:
        if not isinstance(value, str):
            return None

        normalized = " ".join(value.split())
        return normalized or None

    @staticmethod
    def _looks_like_answer_question(message: str) -> bool:
        normalized = message.strip().lower()

        if GeminiIntentProvider._looks_like_content_to_save(message):
            return False

        if "http://" in normalized or "https://" in normalized:
            return False

        explicit_save_markers = {
            "kaydet",
            "not al",
            "kendime not",
            "bunu sakla",
            "bunu kaydet",
            "save this",
        }
        if any(marker in normalized for marker in explicit_save_markers):
            return False

        question_signals = {
            "?",
            " nedir",
            " nasil",
            " nasıl",
            " neden",
            " ne kadar",
            " kac ",
            " kaç ",
            " hangi",
            " hangisi",
            " almaliyim",
            " almalıyım",
            " onerir",
            " önerir",
            " mi ",
            " mı ",
            " mu ",
            " mü ",
        }

        return any(signal in f" {normalized} " for signal in question_signals)

    @staticmethod
    def _looks_like_content_to_save(message: str) -> bool:
        normalized = message.strip().lower()

        content_markers = {
            "başlık:",
            "baslik:",
            "title:",
            "özet:",
            "ozet:",
            "ana noktalar",
        }
        if any(marker in normalized for marker in content_markers):
            return True

        if "http://" in normalized or "https://" in normalized:
            return False

        word_count = len(normalized.split())
        sentence_count = normalized.count(".") + normalized.count("!") + normalized.count("?")

        return word_count >= 45 and sentence_count >= 2
