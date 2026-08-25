from __future__ import annotations

import re

from app.contracts.answers import AnswerRequest, AnswerResponse
from app.services.answer_providers.base import AnswerProvider
from app.services.prompt_loader import PromptLoader


class FakeAnswerProvider(AnswerProvider):
    def __init__(self, prompt_loader: PromptLoader) -> None:
        self._prompt_loader = prompt_loader

    def create_answer(self, request: AnswerRequest) -> AnswerResponse:
        self._prompt_loader.load()

        if not request.chunks:
            return AnswerResponse(
                content_id=request.content_id,
                answer=(
                    "Kayıtlarında bu soruya doğrudan cevap verecek yeterli içerik bulamadım. "
                    "Daha net bir anahtar kelime, tarih veya içerik türüyle tekrar deneyebilirsin."
                ),
                used_chunk_indexes=[],
                language="tr",
                provider="fake",
            )

        used_indexes = [chunk.index for chunk in request.chunks[:3]]
        summaries = [
            self._compress_whitespace(chunk.text)[:220].strip()
            for chunk in request.chunks[:3]
            if chunk.text.strip()
        ]
        question = self._compress_whitespace(request.question)

        return AnswerResponse(
            content_id=request.content_id,
            answer=(
                f"Soruna göre en alakalı kayıtlar bunlar: {' '.join(summaries)}. "
                f"Sorunun özü: {question}. "
                "Bunlara dayanarak aradığın bilgi kayıtlarında bulunuyor."
            ).strip(),
            used_chunk_indexes=used_indexes,
            language="tr",
            provider="fake",
        )

    @staticmethod
    def _compress_whitespace(text: str) -> str:
        return " ".join(text.split())
