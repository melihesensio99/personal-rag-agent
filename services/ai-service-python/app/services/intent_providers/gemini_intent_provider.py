from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


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
            f"Today's date is {request.current_date}. "
            "Classify the user's message for a personal content assistant. "
            "Return only JSON. "
            "action must be save_content, list_contents, answer_from_memory, or ask_clarification. "
            "intent must be save, search, or clarify. "
            "For backwards compatibility, intent must be save when action is save_content, search when action is list_contents or answer_from_memory, and clarify when action is ask_clarification. "
            "query is the user's search/question text for list_contents or answer_from_memory. "
            "content is the text to save for save_content. "
            "clarification_message is a short Turkish message for ask_clarification. "
            "content_kind must be text, video, image, or null. "
            "source_type must be article, youtube, pdf, image, telegram, or null. "
            "time_filter must be today, yesterday, two_days_ago, or none. "
            "keywords should contain only meaningful topic words and should not include filler words like getir, listele, attığım, linkleri. For answer_from_memory, semantic_query is required: produce a concise retrieval description preserving the user's topic and entities without adding new claims. For other actions semantic_query must be null. "
            "If the user asks for videos in general, set content_kind to video even when source_type is null. "
            "If the user asks for articles, writings, PDFs, or text-like records in general, set content_kind to text unless a stricter source_type is clearly requested. "
            "If the user asks for images, visuals, photos, or screenshots, set content_kind to image. "
            "If the user wants previously saved records, choose action list_contents. "
            "If the user asks a factual or conceptual question that should be answered from saved knowledge, choose action answer_from_memory even when they do not use retrieve/list/search verbs. "
            "Question signals include: ?, nedir, nasil, nasıl, neden, ne kadar, kac, kaç, hangi, hangisi, onerir, önerir, almaliyim, almalıyım. "
            "A long conceptual question comparing approaches is still a search/answer request, not clarify. "
            "Do not choose save for a standalone question unless the user explicitly says it is a note to save. "
            "If the user sends article-like content, long pasted text, or text starting with Baslik/Başlık/Title, choose save. "
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
