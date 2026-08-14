from functools import lru_cache

from app.config import settings
from app.services.prompt_loader import PromptLoader
from app.services.summary_service import SummaryService


@lru_cache
def get_summary_service() -> SummaryService:
    prompt_loader = PromptLoader(settings.summary_prompt_path)
    return SummaryService(prompt_loader)
