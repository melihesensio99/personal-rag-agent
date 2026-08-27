from functools import lru_cache

from app.config import settings
from app.services.chunking_service import ChunkingService
from app.services.answer_service import AnswerService
from app.services.answer_providers.fake_answer_provider import FakeAnswerProvider
from app.services.answer_providers.gemini_answer_provider import GeminiAnswerProvider
from app.services.answer_providers.mistral_answer_provider import MistralAnswerProvider
from app.services.embedding_service import EmbeddingService
from app.services.extraction_service import ExtractionService
from app.services.intent_service import IntentService
from app.services.prompt_loader import PromptLoader
from app.services.embedding_providers.fake_embedding_provider import FakeEmbeddingProvider
from app.services.embedding_providers.mistral_embedding_provider import MistralEmbeddingProvider
from app.services.extractors.article_extractor import ArticleExtractor
from app.services.extractors.youtube_extractor import YouTubeExtractor
from app.services.intent_providers.fake_intent_provider import FakeIntentProvider
from app.services.intent_providers.gemini_intent_provider import GeminiIntentProvider
from app.services.intent_providers.mistral_intent_provider import MistralIntentProvider
from app.services.summary_service import SummaryService
from app.services.summary_providers.fake_summary_provider import FakeSummaryProvider
from app.services.summary_providers.gemini_summary_provider import GeminiSummaryProvider
from app.services.summary_providers.mistral_summary_provider import MistralSummaryProvider


@lru_cache
def get_summary_service() -> SummaryService:
    prompt_loader = PromptLoader(settings.summary_prompt_path)
    provider_name = settings.summary_provider.lower()

    if provider_name == "gemini":
        if not settings.gemini_api_key.strip():
            raise ValueError("AI_SERVICE_GEMINI_API_KEY must be set when summary_provider=gemini.")

        provider = GeminiSummaryProvider(
            prompt_loader=prompt_loader,
            api_key=settings.gemini_api_key,
            model=settings.gemini_model,
            base_url=settings.gemini_base_url,
            timeout_seconds=settings.gemini_timeout_seconds,
        )
        return SummaryService(provider)

    if provider_name == "mistral":
        if not settings.mistral_api_key.strip():
            raise ValueError("AI_SERVICE_MISTRAL_API_KEY must be set when summary_provider=mistral.")

        provider = MistralSummaryProvider(
            prompt_loader=prompt_loader,
            api_key=settings.mistral_api_key,
            model=settings.mistral_summary_model,
            base_url=settings.mistral_base_url,
            timeout_seconds=settings.mistral_timeout_seconds,
        )
        return SummaryService(provider)

    if provider_name == "fake":
        _ensure_fake_providers_allowed("summary_provider")
        return SummaryService(FakeSummaryProvider(prompt_loader))

    raise ValueError(f"Unsupported summary_provider: {settings.summary_provider}.")


@lru_cache
def get_answer_service() -> AnswerService:
    prompt_loader = PromptLoader(settings.answer_prompt_path)
    provider_name = settings.answer_provider.lower()

    if provider_name == "gemini":
        if not settings.gemini_api_key.strip():
            raise ValueError("AI_SERVICE_GEMINI_API_KEY must be set when answer_provider=gemini.")

        provider = GeminiAnswerProvider(
            prompt_loader=prompt_loader,
            api_key=settings.gemini_api_key,
            model=settings.gemini_model,
            base_url=settings.gemini_base_url,
            timeout_seconds=settings.gemini_timeout_seconds,
        )
        return AnswerService(provider)

    if provider_name == "mistral":
        if not settings.mistral_api_key.strip():
            raise ValueError("AI_SERVICE_MISTRAL_API_KEY must be set when answer_provider=mistral.")

        provider = MistralAnswerProvider(
            prompt_loader=prompt_loader,
            api_key=settings.mistral_api_key,
            model=settings.mistral_answer_model,
            base_url=settings.mistral_base_url,
            timeout_seconds=settings.mistral_timeout_seconds,
        )
        return AnswerService(provider)

    if provider_name == "fake":
        _ensure_fake_providers_allowed("answer_provider")
        return AnswerService(FakeAnswerProvider(prompt_loader))

    raise ValueError(f"Unsupported answer_provider: {settings.answer_provider}.")


@lru_cache
def get_chunking_service() -> ChunkingService:
    return ChunkingService()


@lru_cache
def get_embedding_service() -> EmbeddingService:
    provider_name = settings.embedding_provider.lower()

    if provider_name == "mistral":
        if not settings.mistral_api_key.strip():
            raise ValueError("AI_SERVICE_MISTRAL_API_KEY must be set when embedding_provider=mistral.")

        provider = MistralEmbeddingProvider(
            api_key=settings.mistral_api_key,
            model=settings.mistral_embedding_model,
            base_url=settings.mistral_base_url,
            timeout_seconds=settings.mistral_timeout_seconds,
        )
        return EmbeddingService(provider, expected_dimension=settings.embedding_dimension)

    if provider_name == "fake":
        _ensure_fake_providers_allowed("embedding_provider")
        return EmbeddingService(
            FakeEmbeddingProvider(dimension=settings.embedding_dimension),
            expected_dimension=settings.embedding_dimension,
        )

    raise ValueError(f"Unsupported embedding_provider: {settings.embedding_provider}.")


@lru_cache
def get_extraction_service() -> ExtractionService:
    article_extractor = ArticleExtractor()
    youtube_extractor = YouTubeExtractor()
    return ExtractionService(article_extractor, youtube_extractor)


@lru_cache
def get_intent_service() -> IntentService:
    provider_name = settings.intent_provider.lower()

    if provider_name == "gemini":
        if not settings.gemini_api_key.strip():
            raise ValueError("AI_SERVICE_GEMINI_API_KEY must be set when intent_provider=gemini.")

        provider = GeminiIntentProvider(
            api_key=settings.gemini_api_key,
            model=settings.gemini_model,
            base_url=settings.gemini_base_url,
            timeout_seconds=settings.gemini_timeout_seconds,
        )
        return IntentService(provider)

    if provider_name == "mistral":
        if not settings.mistral_api_key.strip():
            raise ValueError("AI_SERVICE_MISTRAL_API_KEY must be set when intent_provider=mistral.")

        provider = MistralIntentProvider(
            api_key=settings.mistral_api_key,
            model=settings.mistral_intent_model,
            base_url=settings.mistral_base_url,
            timeout_seconds=settings.mistral_timeout_seconds,
        )
        return IntentService(provider)

    if provider_name == "fake":
        _ensure_fake_providers_allowed("intent_provider")
        return IntentService(FakeIntentProvider())

    raise ValueError(f"Unsupported intent_provider: {settings.intent_provider}.")


def _ensure_fake_providers_allowed(setting_name: str) -> None:
    if not settings.allow_fake_providers:
        raise ValueError(
            f"{setting_name}=fake is only allowed when AI_SERVICE_ALLOW_FAKE_PROVIDERS=true."
        )
