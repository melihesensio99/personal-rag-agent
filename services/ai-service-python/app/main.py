from fastapi import FastAPI

from app.config import settings
from app.routers.health import router as health_router
from app.routers.summaries import router as summaries_router

app = FastAPI(
    title="Telegram AI Service",
    version=settings.service_version,
)

app.include_router(health_router)
app.include_router(summaries_router)
