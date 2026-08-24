from app.contracts.embeddings import EmbeddingRequest, EmbeddingResponse, TextEmbedding
from app.services.embedding_providers.base import EmbeddingProvider


class EmbeddingService:
    def __init__(self, provider: EmbeddingProvider, expected_dimension: int) -> None:
        self._provider = provider
        self._expected_dimension = expected_dimension

    def create_embeddings(self, request: EmbeddingRequest) -> EmbeddingResponse:
        normalized_texts = [" ".join(text.split()) for text in request.texts]
        model, embeddings = self._provider.create_embeddings(normalized_texts)

        if len(embeddings) != len(normalized_texts):
            raise ValueError("Embedding provider returned a different number of embeddings.")

        if not embeddings:
            raise ValueError("Embedding provider returned no embeddings.")

        dimension = len(embeddings[0])
        if dimension != self._expected_dimension:
            raise ValueError(
                f"Embedding dimension mismatch. Expected {self._expected_dimension}, got {dimension}."
            )

        for embedding in embeddings:
            if len(embedding) != dimension:
                raise ValueError("Embedding provider returned inconsistent dimensions.")

        return EmbeddingResponse(
            content_id=request.content_id,
            model=model,
            dimension=dimension,
            embeddings=[
                TextEmbedding(index=index, embedding=embedding)
                for index, embedding in enumerate(embeddings)
            ],
        )
