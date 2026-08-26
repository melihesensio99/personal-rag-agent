from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


class MistralIntentProvider(IntentProvider):
    MAX_ATTEMPTS = 3

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
        last_error: Exception | None = None

        for _ in range(self.MAX_ATTEMPTS):
            try:
                payload = self._send_request(request)
                output_text = self._extract_output_text(payload)

                if not isinstance(output_text, str) or not output_text.strip():
                    raise ValueError("Mistral intent response did not include output text.")

                parsed = self._parse_json_object(output_text)

                response = IntentResponse(
                    intent=self._normalize_required_literal(
                        parsed.get("intent"),
                        self._INTENT_ALIASES,
                        fallback="clarify",
                    ),
                    content_kind=self._normalize_optional_literal(
                        parsed.get("content_kind"),
                        self._CONTENT_KIND_ALIASES,
                    ),
                    source_type=self._normalize_optional_literal(
                        parsed.get("source_type"),
                        self._SOURCE_TYPE_ALIASES,
                    ),
                    time_filter=self._normalize_required_literal(
                        parsed.get("time_filter", "none"),
                        self._TIME_FILTER_ALIASES,
                        fallback="none",
                    ),
                    keywords=self._normalize_keywords(parsed.get("keywords", [])),
                    needs_clarification=self._normalize_bool(parsed.get("needs_clarification", False)),
                )

                if response.intent != "save" and self._looks_like_content_to_save(request.message):
                    return response.model_copy(
                        update={
                            "intent": "save",
                            "content_kind": "text",
                            "source_type": "telegram",
                            "time_filter": "none",
                            "keywords": [],
                            "needs_clarification": False,
                        }
                    )

                if response.intent in {"save", "clarify"} and self._looks_like_answer_question(request.message):
                    return response.model_copy(
                        update={
                            "intent": "search",
                            "content_kind": None,
                            "source_type": None,
                            "time_filter": "none",
                            "needs_clarification": False,
                        }
                    )

                return response
            except (json.JSONDecodeError, RuntimeError, ValueError) as error:
                last_error = error

        raise RuntimeError("Mistral intent failed after retries.") from last_error

    def _send_request(self, request: IntentRequest) -> dict[str, object]:
        endpoint = f"{self._base_url}/chat/completions"

        system_prompt = (
            "You classify messages for a personal content assistant. "
            "Return only a JSON object. "
            "intent must be save, search, or clarify. "
            "content_kind must be text, video, image, or null. "
            "source_type must be article, youtube, pdf, image, telegram, or null. "
            "time_filter must be today, yesterday, two_days_ago, or none. "
            "keywords must contain only meaningful topic words. "
            "The user usually writes in Turkish. "
            "Turkish search verbs include: getir, listele, göster, goster, bul, ara, neydi, hangisiydi. "
            "If the user asks to retrieve, list, show, find, or search previously saved records, choose search. "
            "If the user asks a factual question that should be answered from saved knowledge, choose search even when they do not use retrieve/list/search verbs. "
            "Question signals include: ?, nedir, nasil, nasıl, neden, ne kadar, kac, kaç, hangi, hangisi, onerir, önerir, almaliyim, almalıyım. "
            "A long conceptual question comparing approaches is still a search/answer request, not clarify. "
            "Do not choose save for a standalone question unless the user explicitly says it is a note to save. "
            "If the user sends article-like content, long pasted text, or text starting with Baslik/Başlık/Title, choose save. "
            "If the user sends a URL, article text, note, or content to save without asking to retrieve old records, choose save. "
            "If the user asks for videos in general, set content_kind to video and source_type to null unless YouTube is explicitly requested. "
            "If the user says youtube, set source_type to youtube and content_kind to video. "
            "If the user says makale or article, set source_type to article and content_kind to null unless they explicitly ask for text content in general. "
            "If the user says yazı, yazi, pdf, doküman, or dokuman, set content_kind to text. "
            "If the user says bugün or bugun, set time_filter to today. "
            "If the user says dün or dun, set time_filter to yesterday. "
            "Do not include filler words in keywords: bugun, bugün, dun, dün, attigim, attığım, getir, listele, göster, goster, bul, link, video, videolar. "
            "If the message is ambiguous and you cannot safely choose save/search, choose clarify. "
            "Examples: "
            "User: 'bugün attığım spor videolarını listele' => "
            "{\"intent\":\"search\",\"content_kind\":\"video\",\"source_type\":null,\"time_filter\":\"today\",\"keywords\":[\"spor\"],\"needs_clarification\":false}. "
            "User: 'geçen gün attığım sporla ilgili şeyi bulsana' => "
            "{\"intent\":\"search\",\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"spor\"],\"needs_clarification\":false}. "
            "User: 'bugün attığım makaleleri getir' => "
            "{\"intent\":\"search\",\"content_kind\":null,\"source_type\":\"article\",\"time_filter\":\"today\",\"keywords\":[],\"needs_clarification\":false}. "
            "User: 'Kas yapmak için günlük ne kadar protein almalıyım?' => "
            "{\"intent\":\"search\",\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"kas yapmak\",\"protein\"],\"needs_clarification\":false}. "
            "User: 'RAG nedir?' => "
            "{\"intent\":\"search\",\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"rag\"],\"needs_clarification\":false}. "
            "User: 'Geniş doküman kümesinde RAG kurarken indeksleme aşamasında mı derinleşmeliyiz yoksa inference anında aramaya mı güvenmeliyiz?' => "
            "{\"intent\":\"search\",\"content_kind\":null,\"source_type\":null,\"time_filter\":\"none\",\"keywords\":[\"rag\",\"indexing\",\"inference\",\"retrieval\"],\"needs_clarification\":false}. "
            "User: 'Başlık: Sabah Antrenmanı Daha Verimlidir Sabah saatlerinde yapılan antrenmanlar...' => "
            "{\"intent\":\"save\",\"content_kind\":\"text\",\"source_type\":\"telegram\",\"time_filter\":\"none\",\"keywords\":[],\"needs_clarification\":false}. "
            "User: 'kendime not: RAG chunking önemli' => "
            "{\"intent\":\"save\",\"content_kind\":\"text\",\"source_type\":\"telegram\",\"time_filter\":\"none\",\"keywords\":[],\"needs_clarification\":false}."
        )

        user_prompt = (
            f"Today's date is {request.current_date}.\n"
            f"User message: {request.message}"
        )

        body = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            "temperature": 0.1,
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
