from __future__ import annotations

from dataclasses import dataclass
from importlib import import_module
from typing import Any


@dataclass(frozen=True)
class YouTubeTranscriptResult:
    status: str
    text: str = ""
    language: str | None = None
    reason: str | None = None


class YouTubeTranscriptProvider:
    def fetch_transcript(self, video_id: str) -> YouTubeTranscriptResult:
        transcript_api = self._load_transcript_api()
        if transcript_api is None:
            return YouTubeTranscriptResult(status="dependency_missing", reason="youtube_transcript_api_not_installed")

        try:
            entries = self._fetch_entries(transcript_api, video_id)
        except Exception as error:
            return YouTubeTranscriptResult(status="unavailable", reason=type(error).__name__)

        text = self._normalize_entries(entries)
        if not text:
            return YouTubeTranscriptResult(status="empty", reason="transcript_text_empty")

        return YouTubeTranscriptResult(status="completed", text=text, language=self._detect_language(entries))

    def _load_transcript_api(self) -> Any | None:
        try:
            module = import_module("youtube_transcript_api")
        except ModuleNotFoundError:
            return None

        return getattr(module, "YouTubeTranscriptApi", None)

    def _fetch_entries(self, transcript_api: Any, video_id: str) -> list[Any]:
        languages = ["tr", "en"]

        if hasattr(transcript_api, "get_transcript"):
            return transcript_api.get_transcript(video_id, languages=languages)

        api_instance = transcript_api()
        transcript = api_instance.fetch(video_id, languages=languages)
        return list(transcript)

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
