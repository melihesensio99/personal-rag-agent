from fastapi import FastAPI

from app.config import settings
from app.routers.chunks import router as chunks_router
from app.routers.extractions import router as extractions_router
from app.routers.health import router as health_router
from app.routers.intents import router as intents_router
from app.routers.summaries import router as summaries_router

app = FastAPI(
    title="Telegram AI Service",
    version=settings.service_version,
)

app.include_router(health_router)
app.include_router(chunks_router)
app.include_router(extractions_router)
app.include_router(intents_router)
app.include_router(summaries_router)
