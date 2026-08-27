import re

from app.contracts.intents import IntentRequest, IntentResponse
from app.services.intent_providers.base import IntentProvider


class FakeIntentProvider(IntentProvider):
    def classify(self, request: IntentRequest) -> IntentResponse:
        lowered = request.message.lower().strip()
        question_signals = ["?", "nedir", "neden", "nasil", "nasıl", "ne kadar", "hangi", "hangisi", " mi ", " mı ", " mu ", " mü "]

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

            content_kind = None
            if any(token in lowered for token in ["video", "videolar", "videolari", "videoları", "reel", "short"]):
                content_kind = "video"
            elif any(token in lowered for token in ["foto", "görsel", "gorsel", "resim", "image"]):
                content_kind = "image"
            elif any(token in lowered for token in ["yazı", "yazi", "makale", "article", "pdf", "doküman", "dokuman"]):
                content_kind = "text"

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
                action="list_contents",
                intent="search",
                query=request.message,
                content_kind=content_kind,
                source_type=source_type,
                time_filter=time_filter,
                keywords=keywords,
                needs_clarification=False,
            )

        if any(signal in f" {lowered} " for signal in question_signals):
            return IntentResponse(
                action="answer_from_memory",
                intent="search",
                query=request.message,
                source_type=None,
                time_filter="none",
                keywords=[],
                needs_clarification=False,
            )

        return IntentResponse(
            action="save_content",
            intent="save",
            content=request.message,
            source_type=None,
            time_filter="none",
            keywords=[],
            needs_clarification=False,
        )
