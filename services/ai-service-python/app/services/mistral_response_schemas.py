from __future__ import annotations

import os


def response_format(name: str, schema: dict[str, object]) -> dict[str, object]:
    if os.getenv("AI_SERVICE_MISTRAL_RESPONSE_FORMAT", "json_schema").strip().lower() == "json_object":
        return {"type": "json_object"}

    return {
        "type": "json_schema",
        "json_schema": {"name": name, "schema": schema},
    }


ANSWER_SCHEMA: dict[str, object] = {
    "type": "object",
    "properties": {
        "answer": {"type": "string"},
        "used_chunk_indexes": {"type": "array", "items": {"type": "integer"}},
        "language": {"type": "string", "enum": ["tr"]},
    },
    "required": ["answer", "used_chunk_indexes", "language"],
    "additionalProperties": False,
}

SUMMARY_SCHEMA: dict[str, object] = {
    "type": "object",
    "properties": {
        "title": {"type": "string"},
        "short_summary": {"type": "string"},
        "key_points": {"type": "array", "items": {"type": "string"}},
        "tags": {"type": "array", "items": {"type": "string"}},
        "language": {"type": "string", "enum": ["tr"]},
    },
    "required": ["title", "short_summary", "key_points", "tags", "language"],
    "additionalProperties": False,
}

INTENT_SCHEMA: dict[str, object] = {
    "type": "object",
    "properties": {
        "action": {"type": "string", "enum": ["save_content", "list_contents", "answer_from_memory", "ask_clarification"]},
        "intent": {"type": "string", "enum": ["save", "search", "clarify"]},
        "query": {"type": ["string", "null"]},
        "content": {"type": ["string", "null"]},
        "content_kind": {"type": ["string", "null"], "enum": ["text", "video", "image", None]},
        "source_type": {"type": ["string", "null"], "enum": ["article", "youtube", "pdf", "image", "telegram", None]},
        "time_filter": {"type": "string", "enum": ["today", "yesterday", "two_days_ago", "none"]},
        "date_from": {"type": ["string", "null"]},
        "date_to": {"type": ["string", "null"]},
        "keywords": {"type": "array", "items": {"type": "string"}},
        "semantic_query": {"type": ["string", "null"]},
        "needs_clarification": {"type": "boolean"},
        "clarification_message": {"type": ["string", "null"]},
    },
    "required": ["action", "intent", "query", "content", "content_kind", "source_type", "time_filter", "date_from", "date_to", "keywords", "semantic_query", "needs_clarification", "clarification_message"],
    "additionalProperties": False,
}
