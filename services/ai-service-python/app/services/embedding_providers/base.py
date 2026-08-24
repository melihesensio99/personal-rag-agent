from abc import ABC, abstractmethod


class EmbeddingProvider(ABC):
    @abstractmethod
    def create_embeddings(self, texts: list[str]) -> tuple[str, list[list[float]]]:
        raise NotImplementedError
