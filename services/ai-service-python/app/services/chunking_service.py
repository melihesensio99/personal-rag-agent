from app.contracts.chunks import ChunkRequest, ChunkResponse, TextChunk


class ChunkingService:
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
            end = min(start + chunk_size, text_length)
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
        normalized = " ".join(text.split())
        if not normalized:
            raise ValueError("chunk input cannot be empty")

        return normalized
