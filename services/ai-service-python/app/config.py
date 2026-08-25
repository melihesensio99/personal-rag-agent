from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    service_name: str = "ai-service-python"
    service_version: str = "1.0"
    summary_prompt_path: str = "app/prompts/content_summary_v1.txt"
    answer_prompt_path: str = "app/prompts/content_answer_v1.txt"
    summary_provider: str = "fake"
    answer_provider: str = "fake"
    intent_provider: str = "fake"
    embedding_provider: str = "fake"
    embedding_dimension: int = 1024
    gemini_api_key: str = ""
    gemini_model: str = "gemini-3.6-flash"
    gemini_base_url: str = "https://generativelanguage.googleapis.com/v1beta"
    gemini_timeout_seconds: int = 30
    mistral_api_key: str = ""
    mistral_base_url: str = "https://api.mistral.ai/v1"
    mistral_intent_model: str = "ministral-3b-2512"
    mistral_summary_model: str = "ministral-8b-2512"
    mistral_answer_model: str = "ministral-8b-2512"
    mistral_embedding_model: str = "mistral-embed-2312"
    mistral_timeout_seconds: int = 20

    model_config = SettingsConfigDict(
        env_prefix="AI_SERVICE_",
        env_file=".env",
        extra="ignore",
    )


settings = Settings()
