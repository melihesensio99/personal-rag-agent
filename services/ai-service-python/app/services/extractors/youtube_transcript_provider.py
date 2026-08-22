from __future__ import annotations

from dataclasses import dataclass
from importlib import import_module
from typing import Any


@dataclass(frozen=True)
class YouTubeTranscriptResult:
    status: str
    text: str = ""
    language: str | None = None
    language_name: str | None = None
    is_generated: bool | None = None
    reason: str | None = None


class YouTubeTranscriptProvider:
    PREFERRED_LANGUAGES = ["tr", "en", "hi", "hi-IN"]

    def fetch_transcript(self, video_id: str) -> YouTubeTranscriptResult:
        transcript_api = self._load_transcript_api()
        if transcript_api is None:
            return YouTubeTranscriptResult(status="dependency_missing", reason="youtube_transcript_api_not_installed")

        try:
            entries, language, language_name, is_generated = self._fetch_entries(transcript_api, video_id)
        except Exception as error:
            return YouTubeTranscriptResult(status="unavailable", reason=type(error).__name__)

        text = self._normalize_entries(entries)
        if not text:
            return YouTubeTranscriptResult(status="empty", reason="transcript_text_empty")

        return YouTubeTranscriptResult(
            status="completed",
            text=text,
            language=language or self._detect_language(entries),
            language_name=language_name,
            is_generated=is_generated,
        )

    def _load_transcript_api(self) -> Any | None:
        try:
            module = import_module("youtube_transcript_api")
        except ModuleNotFoundError:
            return None

        return getattr(module, "YouTubeTranscriptApi", None)

    def _fetch_entries(self, transcript_api: Any, video_id: str) -> tuple[list[Any], str | None, str | None, bool | None]:
        api_instance = transcript_api()

        if hasattr(api_instance, "list"):
            transcript = self._select_listed_transcript(api_instance, video_id)
            if transcript is not None:
                return (
                    list(transcript.fetch()),
                    self._read_transcript_language_code(transcript),
                    self._read_transcript_language_name(transcript),
                    self._read_transcript_is_generated(transcript),
                )

        if hasattr(transcript_api, "get_transcript"):
            return (
                transcript_api.get_transcript(video_id, languages=self.PREFERRED_LANGUAGES),
                None,
                None,
                None,
            )

        transcript = api_instance.fetch(video_id, languages=self.PREFERRED_LANGUAGES)
        return (list(transcript), None, None, None)

    def _select_listed_transcript(self, api_instance: Any, video_id: str) -> Any | None:
        try:
            transcript_list = api_instance.list(video_id)
        except Exception:
            return None

        return self._find_first_transcript(transcript_list)

    def _find_first_transcript(self, transcript_list: Any) -> Any | None:
        transcripts = list(transcript_list)
        if not transcripts:
            return None

        for language in self.PREFERRED_LANGUAGES:
            for transcript in transcripts:
                if self._read_transcript_language_code(transcript) == language:
                    return transcript

        return transcripts[0]

    def _normalize_entries(self, entries: list[Any]) -> str:
        parts: list[str] = []

        for entry in entries:
            text = self._read_entry_text(entry)
            if text:
                parts.append(text)

        return " ".join(" ".join(parts).split())

    def _read_entry_text(self, entry: Any) -> str | None:
        if isinstance(entry, dict):
            value = entry.get("text")
            return value.strip() if isinstance(value, str) and value.strip() else None

        value = getattr(entry, "text", None)
        return value.strip() if isinstance(value, str) and value.strip() else None

    def _detect_language(self, entries: list[Any]) -> str | None:
        for entry in entries:
            if isinstance(entry, dict):
                value = entry.get("language_code")
            else:
                value = getattr(entry, "language_code", None)

            if isinstance(value, str) and value.strip():
                return value.strip()

        return None

    def _read_transcript_language_code(self, transcript: Any) -> str | None:
        value = getattr(transcript, "language_code", None)
        return value.strip() if isinstance(value, str) and value.strip() else None

    def _read_transcript_language_name(self, transcript: Any) -> str | None:
        value = getattr(transcript, "language", None)
        return value.strip() if isinstance(value, str) and value.strip() else None

    def _read_transcript_is_generated(self, transcript: Any) -> bool | None:
        value = getattr(transcript, "is_generated", None)
        return value if isinstance(value, bool) else None
