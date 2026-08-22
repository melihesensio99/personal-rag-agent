from functools import lru_cache

from app.config import settings
from app.services.chunking_service import ChunkingService
from app.services.extraction_service import ExtractionService
from app.services.intent_service import IntentService
from app.services.prompt_loader import PromptLoader
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

    if settings.summary_provider.lower() == "gemini":
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

    if settings.summary_provider.lower() == "mistral":
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

    return SummaryService(FakeSummaryProvider(prompt_loader))


@lru_cache
def get_chunking_service() -> ChunkingService:
    return ChunkingService()


@lru_cache
def get_extraction_service() -> ExtractionService:
    article_extractor = ArticleExtractor()
    youtube_extractor = YouTubeExtractor()
    return ExtractionService(article_extractor, youtube_extractor)


@lru_cache
def get_intent_service() -> IntentService:
    if settings.intent_provider.lower() == "gemini":
        if not settings.gemini_api_key.strip():
            raise ValueError("AI_SERVICE_GEMINI_API_KEY must be set when intent_provider=gemini.")

        provider = GeminiIntentProvider(
            api_key=settings.gemini_api_key,
            model=settings.gemini_model,
            base_url=settings.gemini_base_url,
            timeout_seconds=settings.gemini_timeout_seconds,
        )
        return IntentService(provider)

    if settings.intent_provider.lower() == "mistral":
        if not settings.mistral_api_key.strip():
            raise ValueError("AI_SERVICE_MISTRAL_API_KEY must be set when intent_provider=mistral.")

        provider = MistralIntentProvider(
            api_key=settings.mistral_api_key,
            model=settings.mistral_intent_model,
            base_url=settings.mistral_base_url,
            timeout_seconds=settings.mistral_timeout_seconds,
        )
        return IntentService(provider)

    return IntentService(FakeIntentProvider())
