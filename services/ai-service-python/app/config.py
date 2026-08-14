from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    service_name: str = "ai-service-python"
    service_version: str = "1.0"
    summary_prompt_path: str = "app/prompts/content_summary_v1.txt"

    model_config = SettingsConfigDict(
        env_prefix="AI_SERVICE_",
        env_file=".env",
        extra="ignore",
    )


settings = Settings()
