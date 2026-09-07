from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from pydantic import ValidationError

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider
from app.services.prompt_loader import PromptLoader
from app.services.structured_output_runner import run_with_retries
from app.services.mistral_response_schemas import INTENT_SCHEMA, response_format


class MistralIntentProvider(IntentProvider):
    MAX_ATTEMPTS = 3

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
    _INTENT_ALIASES = {
        "save": "save",
        "kaydet": "save",
        "kayıt": "save",
        "record": "save",
        "store": "save",
        "search": "search",
        "retrieve": "search",
        "list": "search",
        "find": "search",
        "get": "search",
        "ara": "search",
        "arama": "search",
        "bul": "search",
        "listele": "search",
        "getir": "search",
        "göster": "search",
        "goster": "search",
        "clarify": "clarify",
        "ask_clarification": "clarify",
        "unclear": "clarify",
        "belirsiz": "clarify",
    }
    _CONTENT_KIND_ALIASES = {
        "text": "text",
        "article": "text",
        "pdf": "text",
        "document": "text",
        "dokuman": "text",
        "doküman": "text",
        "yazi": "text",
        "yazı": "text",
        "video": "video",
        "youtube": "video",
        "reel": "video",
        "short": "video",
        "image": "image",
        "photo": "image",
        "picture": "image",
        "gorsel": "image",
        "görsel": "image",
        "resim": "image",
        "foto": "image",
    }
    _SOURCE_TYPE_ALIASES = {
        "article": "article",
        "makale": "article",
        "youtube": "youtube",
        "pdf": "pdf",
        "image": "image",
        "gorsel": "image",
        "görsel": "image",
        "telegram": "telegram",
    }
    _TIME_FILTER_ALIASES = {
        "today": "today",
        "bugun": "today",
        "bugün": "today",
        "yesterday": "yesterday",
        "dun": "yesterday",
        "dün": "yesterday",
        "two_days_ago": "two_days_ago",
        "2_days_ago": "two_days_ago",
        "iki_gun_once": "two_days_ago",
        "none": "none",
        "null": "none",
        "": "none",
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
        def operation(repair_hint: str | None) -> IntentResponse:
            payload = self._send_request(request, repair_hint)
            output_text = self._extract_output_text(payload)
            if not isinstance(output_text, str) or not output_text.strip():
                raise ValueError("Mistral intent response did not include output text.")

            parsed = self._parse_json_object(output_text)
            response = IntentResponse.model_validate(parsed)
            date_from, date_to = self._normalize_date_range(
                response.date_from,
                response.date_to,
                request.message,
                request.current_date,
            )
            if response.action == "list_contents" and (date_from is None) != (date_to is None):
                raise ValueError("date_from and date_to must either both be set or both be null.")
            normalized_keywords = self._normalize_keywords(response.keywords)
            # For listing requests, keywords must come from the user's text.
            # This prevents the LLM from inventing a topic (for example
            # ``spor`` in "dün attığım videoları listele") and accidentally
            # filtering out otherwise valid records.
            if response.action == "list_contents":
                normalized_keywords = [
                    keyword
                    for keyword in normalized_keywords
                    if self._keyword_appears_in_message(keyword, request.message)
                ]

            return response.model_copy(
                update={
                    "query": (
                        self._normalize_optional_text(response.query) or request.message
                        if response.action in {"list_contents", "answer_from_memory"}
                        else None
                    ),
                    "content": (
                        self._normalize_optional_text(response.content) or request.message
                        if response.action == "save_content"
                        else None
                    ),
                    "content_kind": self._normalize_optional_literal(response.content_kind, self._CONTENT_KIND_ALIASES),
                    "source_type": self._normalize_optional_literal(response.source_type, self._SOURCE_TYPE_ALIASES),
                    "time_filter": self._normalize_time_filter(response.time_filter, request.message),
                    "date_from": date_from,
                    "date_to": date_to,
                    "keywords": normalized_keywords,
            "semantic_query": (
                self._normalize_optional_text(response.semantic_query)
                if response.action == "answer_from_memory"
                else None
            ),
                    "needs_clarification": self._normalize_bool(response.needs_clarification),
                    "clarification_message": self._normalize_optional_text(response.clarification_message),
                }
            )

        return run_with_retries(
            operation,
            lambda error: self._build_repair_hint(error, request.message),
            max_attempts=self.MAX_ATTEMPTS,
            failure_message="Mistral intent failed after retries.",
            retryable_errors=(json.JSONDecodeError, RuntimeError, ValueError, ValidationError),
        )

    def _send_request(self, request: IntentRequest, repair_hint: str | None = None) -> dict[str, object]:
        endpoint = f"{self._base_url}/chat/completions"

        system_prompt = self._prompt_loader.load()

        if repair_hint is not None:
            system_prompt = (
                system_prompt
                + " The previous output was invalid. Fix the schema errors and return only corrected JSON."
            )

        user_prompt = (
            f"Today's date is {request.current_date}.\n"
            f"User message: {request.message}"
        )

        if repair_hint is not None:
            user_prompt = (
                user_prompt
                + "\n\nPrevious output validation errors:\n"
                + repair_hint
                + "\n\nReturn only the corrected JSON object."
            )

        body = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            "temperature": 0.1,
            "response_format": response_format("intent_response", INTENT_SCHEMA),
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
            raise RuntimeError(f"Mistral intent request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Mistral intent request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Mistral intent request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Mistral intent response payload was invalid.")

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
            raise ValueError("Mistral intent JSON output was not an object.")

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
    def _normalize_required_literal(
        value: object,
        aliases: dict[str, str],
        fallback: str,
    ) -> str:
        if value is None:
            return fallback

        normalized = str(value).strip().lower().replace(" ", "_")
        return aliases.get(normalized, fallback)

    @staticmethod
    def _normalize_optional_literal(
        value: object,
        aliases: dict[str, str],
    ) -> str | None:
        if value is None:
            return None

        normalized = str(value).strip().lower().replace(" ", "_")
        if normalized in {"", "none", "null", "unknown", "any", "all", "herhangi"}:
            return None

        return aliases.get(normalized)

    @staticmethod
    def _normalize_optional_text(value: object) -> str | None:
        if not isinstance(value, str):
            return None

        normalized = " ".join(value.split())
        return normalized or None

    @staticmethod
    def _normalize_date(value: object) -> str | None:
        if not isinstance(value, str):
            return None
        normalized = value.strip()
        if len(normalized) != 10:
            return None
        try:
            from datetime import date
            date.fromisoformat(normalized)
        except ValueError:
            return None
        return normalized

    @classmethod
    def _normalize_time_filter(cls, value: object, message: str) -> str:
        normalized = cls._normalize_required_literal(value, cls._TIME_FILTER_ALIASES, fallback="none")
        text = message.strip().lower()
        has_explicit_date = any(
            marker in text
            for marker in ("bugün", "bugun", "dün", "dun", "geçen hafta", "gecen hafta", "geçen ay", "gecen ay")
        )
        return normalized if has_explicit_date else "none"

    @classmethod
    def _normalize_date_range(
        cls,
        date_from: object,
        date_to: object,
        message: str,
        current_date: str,
    ) -> tuple[str | None, str | None]:
        from datetime import date, timedelta

        try:
            today = date.fromisoformat(current_date)
        except ValueError:
            today = date.today()

        text = message.lower()
        normalized_from = cls._normalize_date(date_from)
        normalized_to = cls._normalize_date(date_to)

        # Relative date expressions are resolved once here, so the backend
        # only needs to execute the resulting ISO date range.
        if "bugün" in text or "bugun" in text:
            return today.isoformat(), (today + timedelta(days=1)).isoformat()
        if "dün" in text or "dun" in text:
            return (today - timedelta(days=1)).isoformat(), today.isoformat()
        if "iki gün önce" in text or "iki gun once" in text:
            return (today - timedelta(days=2)).isoformat(), (today - timedelta(days=1)).isoformat()

        # "Bu hafta" means the current calendar week (Monday through today),
        # not a rolling seven-day window. The upper bound is exclusive.
        if "bu hafta" in text or "buhafta" in text:
            week_start = today - timedelta(days=today.weekday())
            return week_start.isoformat(), (today + timedelta(days=1)).isoformat()

        if "bu ay" in text or "buay" in text:
            month_start = today.replace(day=1)
            return month_start.isoformat(), (today + timedelta(days=1)).isoformat()

        import re
        match = re.search(r"(\d+)\s*(?:hafta|haft)\s+önce(?:ki)?", text)
        if match:
            current_week_start = today - timedelta(days=today.weekday())
            target_week_start = current_week_start - timedelta(weeks=int(match.group(1)))
            return target_week_start.isoformat(), (target_week_start + timedelta(days=7)).isoformat()

        match = re.search(r"(\d+)\s*ay\s+önce(?:ki)?", text)
        if match:
            months_back = int(match.group(1))
            absolute_month = today.year * 12 + (today.month - 1) - months_back
            target_year, target_month_zero_based = divmod(absolute_month, 12)
            target_month_start = date(target_year, target_month_zero_based + 1, 1)
            next_month = target_month_start.replace(day=28) + timedelta(days=4)
            target_month_end = next_month.replace(day=1)
            return target_month_start.isoformat(), target_month_end.isoformat()

        match = re.search(
            r"(?:son\s+|yaklaşık\s+|yaklasik\s+)?(\d+)\s*(?:gün|gun|gn)\s+önce(?:ki)?",
            text,
        )
        if match:
            start = today - timedelta(days=int(match.group(1)))
            return start.isoformat(), (start + timedelta(days=1)).isoformat()

        match = re.search(r"son\s+(\d+)\s+gün", text)
        if match:
            return (today - timedelta(days=int(match.group(1)) - 1)).isoformat(), (today + timedelta(days=1)).isoformat()

        if "geçen ay" in text or "gecen ay" in text:
            this_month = today.replace(day=1)
            previous_month_end = this_month - timedelta(days=1)
            previous_month_start = previous_month_end.replace(day=1)
            return previous_month_start.isoformat(), this_month.isoformat()

        if "geçen hafta" in text or "gecen hafta" in text:
            week_start = today - timedelta(days=today.weekday())
            previous_week_start = week_start - timedelta(days=7)
            return previous_week_start.isoformat(), week_start.isoformat()

        # If the message had no relative expression, preserve valid absolute
        # dates returned by the model. Relative expressions above always win,
        # preventing hallucinated ranges such as 4–8 September for "bu hafta".
        if normalized_from and normalized_to:
            return normalized_from, normalized_to

        return None, None

    @staticmethod
    def _derive_action_from_intent(intent: str, message: str) -> str:
        if intent == "save":
            return "save_content"

        if intent == "clarify":
            return "ask_clarification"

        if MistralIntentProvider._looks_like_answer_question(message):
            return "answer_from_memory"

        return "list_contents"

    @staticmethod
    def _derive_intent_from_action(action: str) -> str:
        if action == "save_content":
            return "save"

        if action == "ask_clarification":
            return "clarify"

        return "search"

    @staticmethod
    def _normalize_keywords(value: object) -> list[str]:
        if not isinstance(value, list):
            return []

        stop_words = {
            "bugun",
            "bugün",
            "dun",
            "dün",
            "attigim",
            "attığım",
            "getir",
            "listele",
            "göster",
            "goster",
            "bul",
            "link",
            "video",
            "videolar",
            "videolari",
            "videoları",
            "makale",
            "makaleler",
            "makaleleri",
            "article",
            "articles",
            "youtube",
            "kayıt",
            "kayıtları",
            "kayit",
            "kayitlari",
        }
        keywords: list[str] = []

        for item in value:
            if not isinstance(item, str):
                continue

            keyword = item.strip().lower()
            if not keyword or keyword in stop_words or keyword in keywords:
                continue

            keywords.append(keyword)

            if len(keywords) == 8:
                break

        return keywords

    @staticmethod
    def _keyword_appears_in_message(keyword: str, message: str) -> bool:
        """Keep list filters only when the user actually supplied the topic.

        Intent models may return plausible but unrelated keywords.  A listing
        query should never gain a new topic from the model because that turns a
        broad date/type request into an empty database search.
        """
        import re
        import unicodedata

        def normalize(value: str) -> str:
            folded = unicodedata.normalize("NFKD", value.lower())
            return "".join(char for char in folded if not unicodedata.combining(char))

        source = normalize(message)
        terms = [term for term in re.findall(r"[\wçğıöşü]+", normalize(keyword)) if len(term) > 1]
        if not terms:
            return False
        source_terms = set(re.findall(r"[\wçğıöşü]+", source))
        # Turkish case suffixes commonly attach to the topic ("sporla",
        # "RAG'la"). A topic is therefore accepted when it is a token prefix.
        return all(any(token.startswith(term) for token in source_terms) for term in terms)

    @staticmethod
    def _normalize_bool(value: object) -> bool:
        if isinstance(value, bool):
            return value

        if isinstance(value, str):
            return value.strip().lower() in {"true", "yes", "evet", "1"}

        return False

    @staticmethod
    def _build_repair_hint(error: Exception, message: str) -> str:
        if isinstance(error, ValidationError):
            parts = []
            for item in error.errors():
                location = ".".join(str(part) for part in item.get("loc", ()))
                detail = item.get("msg", "invalid value")
                parts.append(f"- {location}: {detail}")

            details = "\n".join(parts) if parts else "- Validation failed."
        else:
            details = f"- {error}"

        return (
            "The previous JSON was invalid for this message.\n"
            f"User message: {message}\n"
            f"Validation errors:\n{details}"
        )

    @staticmethod
    def _looks_like_answer_question(message: str) -> bool:
        normalized = message.strip().lower()

        if MistralIntentProvider._looks_like_content_to_save(message):
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
