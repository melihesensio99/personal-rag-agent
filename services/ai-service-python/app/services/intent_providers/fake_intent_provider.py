import re

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


class FakeIntentProvider(IntentProvider):
    def classify(self, request: IntentRequest) -> IntentResponse:
        lowered = request.message.lower().strip()

        if any(token in lowered for token in ["listele", "getir", "göster", "goster", "bul", "kayıt", "kayit"]):
            source_type = None
            if "youtube" in lowered:
                source_type = "youtube"
            elif "pdf" in lowered:
                source_type = "pdf"
            elif "foto" in lowered or "görsel" in lowered or "gorsel" in lowered or "resim" in lowered:
                source_type = "image"
            elif "makale" in lowered or "article" in lowered:
                source_type = "article"

            time_filter = "none"
            if "bugün" in lowered or "bugun" in lowered:
                time_filter = "today"
            elif "dün" in lowered or "dun" in lowered:
                time_filter = "yesterday"
            elif "2 gün" in lowered or "2 gun" in lowered or "iki gün" in lowered or "iki gun" in lowered:
                time_filter = "two_days_ago"

            words = re.findall(r"\w+", lowered)
            stop = {"bugün", "bugun", "dün", "dun", "attığım", "attigim", "listele", "getir", "göster", "goster", "kayıtları", "kayitlari", "kayıt", "kayit", "linkleri", "link", "pdfleri", "youtube", "videolarını", "videolari", "fotoğrafı", "fotografi", "resmi"}
            keywords = [word for word in words if len(word) >= 3 and word not in stop][:5]

            return IntentResponse(
                intent="search",
                source_type=source_type,
                time_filter=time_filter,
                keywords=keywords,
                needs_clarification=False,
            )

        return IntentResponse(intent="save", source_type=None, time_filter="none", keywords=[], needs_clarification=False)
