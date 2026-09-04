from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from pydantic import ValidationError

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider
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
    ) -> None:
        self._api_key = api_key
        self._model = model
        self._base_url = base_url.rstrip("/")
        self._timeout_seconds = timeout_seconds

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
                    "keywords": self._normalize_keywords(response.keywords),
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

        system_prompt = (
            "You classify messages for a personal content assistant. "
            "Return only a JSON object that matches the exact schema. "
            "Do not use aliases, paraphrases, or extra keys. "
            "action must be save_content, list_contents, answer_from_memory, or ask_clarification. "
            "intent must be save, search, or clarify. "
            "query is the user's search/question text for list_contents or answer_from_memory. "
            "content is the text to save for save_content. "
            "clarification_message is a short Turkish message for ask_clarification. "
            "content_kind must be text, video, image, or null. "
            "source_type must be article, youtube, pdf, image, telegram, or null. "
            "time_filter is a legacy compatibility field; keep it as none unless needed for compatibility. "
            "Always resolve any relative or absolute date expression into date_from/date_to as ISO dates (YYYY-MM-DD); date_to is exclusive. "
            "keywords must be an array of meaningful topic words. "
            "For answer_from_memory, semantic_query is required: write a concise natural-language retrieval description that preserves the user's topic, entities, and requested comparison. Do not introduce new topics or claims. For other actions semantic_query must be null. "
            "The user usually writes in Turkish. "
            "Turkish search verbs include: getir, listele, göster, goster, bul, ara, neydi, hangisiydi. "
            "If the user asks to retrieve, list, show, find, or search previously saved records, choose action list_contents. "
            "If the user asks a factual or conceptual question that should be answered from saved knowledge, choose action answer_from_memory even when they do not use retrieve/list/search verbs. "
            "Question signals include: ?, nedir, nasil, nasıl, neden, ne kadar, kac, kaç, hangi, hangisi, onerir, önerir, almaliyim, almalıyım. "
            "A long conceptual question comparing approaches is still a search/answer request, not clarify. "
            "Do not choose save for a standalone question unless the user explicitly says it is a note to save. "
            "If the user sends article-like content, long pasted text, or text starting with Baslik/Başlık/Title, choose save. "
            "If the user sends a URL, article text, note, or content to save without asking to retrieve old records, choose save. "
            "If the user asks for videos in general, set content_kind to video and source_type to null unless YouTube is explicitly requested. "
            "If the user says youtube, set source_type to youtube and content_kind to video. "
            "If the user says makale or article, set source_type to article and content_kind to null unless they explicitly ask for text content in general. "
            "If the user says yazı, yazi, pdf, doküman, or dokuman, set content_kind to text. "
            "If the user says not, notlar, notlarımı, or kendime not, set source_type to telegram and content_kind to text when listing saved notes. "
            "For expressions such as bugün, dün, geçen hafta, geçen ay, 10 gün önce or son 3 gün, calculate date_from/date_to from current_date. "
            "Do not include content-type words or filler words in keywords: makale, makaleler, makaleleri, article, video, videolar, videoları, youtube, link, linkleri, bugun, bugün, dun, dün, attigim, attığım, getir, listele, göster, goster, bul. "
            "If the message is ambiguous and you cannot safely choose save/search, choose clarify. "
            "Examples: "
            "User: 'bugün attığım spor videolarını listele' => "
            "{\"action\":\"list_contents\",\"intent\":\"search\",\"query\":\"bugün attığım spor videolarını listele\",\"content\":null,\"content_kind\":\"video\",\"source_type\":null,\"time_filter\":\"today\",\"date_from\":null,\"date_to\":null,\"keywords\":[\"spor\"],\"semantic_query\":null,\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'geçen gün attığım sporla ilgili şeyi bulsana' => "
            "{\"action\":\"list_contents\",\"intent\":\"search\",\"query\":\"geçen gün attığım sporla ilgili şeyi bulsana\",\"content\":null,\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"date_from\":null,\"date_to\":null,\"keywords\":[\"spor\"],\"semantic_query\":\"spor\",\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'bugün attığım makaleleri getir' => "
            "{\"action\":\"list_contents\",\"intent\":\"search\",\"query\":\"bugün attığım makaleleri getir\",\"content\":null,\"content_kind\":null,\"source_type\":\"article\",\"time_filter\":\"today\",\"date_from\":null,\"date_to\":null,\"keywords\":[],\"semantic_query\":null,\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'Kas yapmak için günlük ne kadar protein almalıyım?' => "
            "{\"action\":\"answer_from_memory\",\"intent\":\"search\",\"query\":\"Kas yapmak için günlük ne kadar protein almalıyım?\",\"content\":null,\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"kas yapmak\",\"protein\"],\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'RAG nedir?' => "
            "{\"action\":\"answer_from_memory\",\"intent\":\"search\",\"query\":\"RAG nedir?\",\"content\":null,\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"rag\"],\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'Geniş doküman kümesinde RAG kurarken indeksleme aşamasında mı derinleşmeliyiz yoksa inference anında aramaya mı güvenmeliyiz?' => "
            "{\"action\":\"answer_from_memory\",\"intent\":\"search\",\"query\":\"Geniş doküman kümesinde RAG kurarken indeksleme aşamasında mı derinleşmeliyiz yoksa inference anında aramaya mı güvenmeliyiz?\",\"content\":null,\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"rag\",\"indexing\",\"inference\",\"retrieval\"],\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'Başlık: Sabah Antrenmanı Daha Verimlidir Sabah saatlerinde yapılan antrenmanlar...' => "
            "{\"action\":\"save_content\",\"intent\":\"save\",\"query\":null,\"content\":\"Başlık: Sabah Antrenmanı Daha Verimlidir Sabah saatlerinde yapılan antrenmanlar...\",\"content_kind\":\"text\",\"source_type\":\"telegram\",\"time_filter\":\"none\",\"keywords\":[],\"needs_clarification\":false,\"clarification_message\":null}. "
            "User: 'kendime not: RAG chunking önemli' => "
            "{\"action\":\"save_content\",\"intent\":\"save\",\"query\":null,\"content\":\"kendime not: RAG chunking önemli\",\"content_kind\":\"text\",\"source_type\":\"telegram\",\"time_filter\":\"none\",\"keywords\":[],\"needs_clarification\":false,\"clarification_message\":null}."
        )

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
        if normalized_from and normalized_to:
            return normalized_from, normalized_to

        # Relative date expressions are resolved once here, so the backend
        # only needs to execute the resulting ISO date range.
        if "bugün" in text or "bugun" in text:
            return today.isoformat(), (today + timedelta(days=1)).isoformat()
        if "dün" in text or "dun" in text:
            return (today - timedelta(days=1)).isoformat(), today.isoformat()
        if "iki gün önce" in text or "iki gun once" in text:
            return (today - timedelta(days=2)).isoformat(), (today - timedelta(days=1)).isoformat()

        import re
        match = re.search(r"(?:son|son\s+|yaklaşık\s+|yaklasik\s+)?(\d+)\s*gün\s+önce", text)
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
