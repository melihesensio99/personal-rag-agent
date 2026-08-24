from __future__ import annotations

import json
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.services.embedding_providers.base import EmbeddingProvider


class MistralEmbeddingProvider(EmbeddingProvider):
    def __init__(
        self,
        api_key: str,
        model: str,
        base_url: str,
        timeout_seconds: int,
    ) -> None:
        self._api_key = api_key
        self._model = model
        self._base_url = base_url.rstrip("/")
        self._timeout_seconds = timeout_seconds

    def create_embeddings(self, texts: list[str]) -> tuple[str, list[list[float]]]:
        payload = self._send_request(texts)
        return self._model, self._extract_embeddings(payload, expected_count=len(texts))

    def _send_request(self, texts: list[str]) -> dict[str, object]:
        endpoint = f"{self._base_url}/embeddings"
        body = {
            "model": self._model,
            "input": texts,
        }

        http_request = Request(
            endpoint,
            data=json.dumps(body).encode("utf-8"),
            headers={
                "Authorization": f"Bearer {self._api_key}",
                "Content-Type": "application/json",
            },
            method="POST",
        )

        try:
            with urlopen(http_request, timeout=self._timeout_seconds) as response:
                raw = response.read().decode("utf-8", errors="ignore")
        except HTTPError as exception:
            details = exception.read().decode("utf-8", errors="ignore")
            raise RuntimeError(f"Mistral embedding request failed with HTTP {exception.code}: {details}") from exception
        except URLError as exception:
            raise RuntimeError(f"Mistral embedding request failed: {exception.reason}") from exception
        except TimeoutError as exception:
            raise RuntimeError("Mistral embedding request timed out.") from exception

        parsed = json.loads(raw)
        if not isinstance(parsed, dict):
            raise ValueError("Mistral embedding response payload was invalid.")

        return parsed

    @staticmethod
    def _extract_embeddings(payload: dict[str, object], expected_count: int) -> list[list[float]]:
        data = payload.get("data")
        if not isinstance(data, list):
            raise ValueError("Mistral embedding response did not include data.")

        embeddings_by_index: dict[int, list[float]] = {}
        for item in data:
            if not isinstance(item, dict):
                continue

            index = item.get("index")
            embedding = item.get("embedding")
            if not isinstance(index, int) or not isinstance(embedding, list):
                continue

            values = [float(value) for value in embedding if isinstance(value, int | float)]
            if not values:
                continue

            embeddings_by_index[index] = values

        embeddings: list[list[float]] = []
        for index in range(expected_count):
            if index not in embeddings_by_index:
                raise ValueError(f"Mistral embedding response is missing index {index}.")

            embeddings.append(embeddings_by_index[index])

        return embeddings
