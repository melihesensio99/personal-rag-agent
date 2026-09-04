from functools import lru_cache
import math

from app.contracts.reranking import RerankRequest, RerankResponse, RerankScore


class RerankingService:
    """Local cross-encoder reranker. The model is loaded once and then reused."""

    MODEL_NAME = "BAAI/bge-reranker-v2-m3"

    def __init__(self) -> None:
        self._model = None

    def rerank(self, request: RerankRequest) -> RerankResponse:
        model = self._get_model()
        pairs = [[request.query, document.text] for document in request.documents]
        raw_scores = model.predict(pairs)
        scores = [
            RerankScore(index=document.index, score=_sigmoid(float(score)))
            for document, score in zip(request.documents, raw_scores, strict=True)
        ]
        return RerankResponse(model=self.MODEL_NAME, scores=scores)

    @lru_cache(maxsize=1)
    def _get_model(self):
        try:
            from sentence_transformers import CrossEncoder
        except ImportError as exception:
            raise RuntimeError(
                "Local reranker requires the sentence-transformers package."
            ) from exception

        return CrossEncoder(self.MODEL_NAME)


def _sigmoid(value: float) -> float:
    """Convert the model's logits to a stable 0..1 relevance score."""
    if value >= 0:
        return 1 / (1 + math.exp(-value))
    exponential = math.exp(value)
    return exponential / (1 + exponential)
