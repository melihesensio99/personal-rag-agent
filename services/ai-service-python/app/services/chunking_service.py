import re

from app.contracts.chunks import ChunkRequest, ChunkResponse, TextChunk


class ChunkingService:
    SENTENCE_BOUNDARIES = frozenset((".", "!", "?", "\n"))

    def create_chunks(self, request: ChunkRequest) -> ChunkResponse:
        normalized_text = self._normalize_text(request.text)
        chunks = self._split_text(
            text=normalized_text,
            chunk_size=request.chunk_size,
            overlap=request.overlap,
        )

        return ChunkResponse(
            content_id=request.content_id,
            chunk_size=request.chunk_size,
            overlap=request.overlap,
            total_chunks=len(chunks),
            chunks=chunks,
        )

    def _split_text(self, text: str, chunk_size: int, overlap: int) -> list[TextChunk]:
        chunks: list[TextChunk] = []
        start = 0
        index = 0
        text_length = len(text)

        while start < text_length:
            end = self._find_chunk_end(text, start, chunk_size)
            chunk_text = text[start:end].strip()

            if chunk_text:
                chunks.append(
                    TextChunk(
                        index=index,
                        text=chunk_text,
                        char_start=start,
                        char_end=end,
                    )
                )
                index += 1

            if end == text_length:
                break

            start = end - overlap

        return chunks

    def _normalize_text(self, text: str) -> str:
        normalized_lines = []
        previous_line_was_blank = False

        for line in text.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
            normalized_line = re.sub(r"[ \t\f\v]+", " ", line).strip()
            if not normalized_line:
                if normalized_lines and not previous_line_was_blank:
                    normalized_lines.append("")
                previous_line_was_blank = True
                continue

            normalized_lines.append(normalized_line)
            previous_line_was_blank = False

        normalized = "\n".join(normalized_lines).strip()
        if not normalized:
            raise ValueError("chunk input cannot be empty")

        return normalized

    def _find_chunk_end(self, text: str, start: int, chunk_size: int) -> int:
        text_length = len(text)
        hard_end = min(start + chunk_size, text_length)
        if hard_end == text_length:
            return hard_end

        minimum_boundary = start + max(1, chunk_size // 2)
        sentence_end = self._find_last_boundary(text, start, hard_end, minimum_boundary)
        if sentence_end is not None:
            return sentence_end

        word_end = self._find_last_word_boundary(text, start, hard_end, minimum_boundary)
        if word_end is not None:
            return word_end

        return hard_end

    def _find_last_boundary(
        self,
        text: str,
        start: int,
        hard_end: int,
        minimum_boundary: int,
    ) -> int | None:
        for index in range(hard_end - 1, start, -1):
            if index + 1 < minimum_boundary:
                return None
            if text[index] in self.SENTENCE_BOUNDARIES:
                return index + 1

        return None

    @staticmethod
    def _find_last_word_boundary(
        text: str,
        start: int,
        hard_end: int,
        minimum_boundary: int,
    ) -> int | None:
        for index in range(hard_end - 1, start, -1):
            if index + 1 < minimum_boundary:
                return None
            if text[index].isspace():
                return index + 1

        return None
