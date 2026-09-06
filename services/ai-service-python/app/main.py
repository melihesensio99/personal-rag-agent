import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.config import settings
from app.dependencies import get_reranking_service
from app.routers.chunks import router as chunks_router
from app.routers.embeddings import router as embeddings_router
from app.routers.extractions import router as extractions_router
from app.routers.answers import router as answers_router
from app.routers.health import router as health_router
from app.routers.intents import router as intents_router
from app.routers.summaries import router as summaries_router
from app.routers.reranking import router as reranking_router


@asynccontextmanager
async def lifespan(_: FastAPI):
    # Loading the cross-encoder can take longer than a normal request timeout.
    # Finish that cold-start work before Uvicorn begins accepting traffic.
    await asyncio.to_thread(get_reranking_service().warm_up)
    yield


app = FastAPI(
    title="Telegram AI Service",
    version=settings.service_version,
    lifespan=lifespan,
)

app.include_router(health_router)
app.include_router(chunks_router)
app.include_router(embeddings_router)
app.include_router(extractions_router)
app.include_router(answers_router)
app.include_router(intents_router)
app.include_router(summaries_router)
app.include_router(reranking_router)
