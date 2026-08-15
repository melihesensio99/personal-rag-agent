from functools import lru_cache

from app.config import settings
from app.services.extraction_service import ExtractionService
from app.services.prompt_loader import PromptLoader
from app.services.extractors.article_extractor import ArticleExtractor
from app.services.summary_service import SummaryService


@lru_cache
def get_summary_service() -> SummaryService:
    prompt_loader = PromptLoader(settings.summary_prompt_path)
    return SummaryService(prompt_loader)


@lru_cache
def get_extraction_service() -> ExtractionService:
    article_extractor = ArticleExtractor()
    return ExtractionService(article_extractor)
