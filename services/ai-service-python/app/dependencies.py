from functools import lru_cache

from app.config import settings
from app.services.chunking_service import ChunkingService
from app.services.answer_service import AnswerService
from app.providers.answer.fake_answer_provider import FakeAnswerProvider
from app.providers.answer.gemini_answer_provider import GeminiAnswerProvider
from app.providers.answer.mistral_answer_provider import MistralAnswerProvider
from app.services.embedding_service import EmbeddingService
from app.services.extraction_service import ExtractionService
from app.services.intent_service import IntentService
from app.utils.prompt_loader import PromptLoader
from app.providers.embedding.fake_embedding_provider import FakeEmbeddingProvider
from app.providers.embedding.mistral_embedding_provider import MistralEmbeddingProvider
from app.providers.extractors.article_extractor import ArticleExtractor
from app.providers.extractors.pmc_article_extractor import PmcArticleExtractor
from app.providers.extractors.pubmed_article_extractor import PubMedArticleExtractor
from app.providers.extractors.youtube_extractor import YouTubeExtractor
from app.providers.intent.fake_intent_provider import FakeIntentProvider
from app.providers.intent.gemini_intent_provider import GeminiIntentProvider
from app.providers.intent.mistral_intent_provider import MistralIntentProvider
from app.services.summary_service import SummaryService
from app.providers.summary.fake_summary_provider import FakeSummaryProvider
from app.providers.summary.gemini_summary_provider import GeminiSummaryProvider
from app.providers.summary.mistral_summary_provider import MistralSummaryProvider
from app.services.reranking_service import RerankingService


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
def get_reranking_service() -> RerankingService:
    return RerankingService()


@lru_cache
def get_extraction_service() -> ExtractionService:
    article_extractor = ArticleExtractor()
    youtube_extractor = YouTubeExtractor()
    return ExtractionService(article_extractor, PmcArticleExtractor(), PubMedArticleExtractor(), youtube_extractor)


@lru_cache
def get_intent_service() -> IntentService:
    provider_name = settings.intent_provider.lower()
    prompt_loader = PromptLoader(settings.intent_prompt_path)

    if provider_name == "gemini":
        if not settings.gemini_api_key.strip():
            raise ValueError("AI_SERVICE_GEMINI_API_KEY must be set when intent_provider=gemini.")

        provider = GeminiIntentProvider(
            prompt_loader=prompt_loader,
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
            prompt_loader=prompt_loader,
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
