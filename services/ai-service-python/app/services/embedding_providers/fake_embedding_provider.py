import hashlib
import math

from app.services.embedding_providers.base import EmbeddingProvider


class FakeEmbeddingProvider(EmbeddingProvider):
    def __init__(self, dimension: int) -> None:
        self._dimension = dimension

    def create_embeddings(self, texts: list[str]) -> tuple[str, list[list[float]]]:
        return "fake", [self._embed_text(text) for text in texts]

    def _embed_text(self, text: str) -> list[float]:
        values: list[float] = []
        seed = text.encode("utf-8")
        counter = 0

        while len(values) < self._dimension:
            digest = hashlib.sha256(seed + counter.to_bytes(4, "big")).digest()
            for byte in digest:
                values.append((byte / 127.5) - 1.0)
                if len(values) == self._dimension:
                    break
            counter += 1

        magnitude = math.sqrt(sum(value * value for value in values)) or 1.0
        return [value / magnitude for value in values]
