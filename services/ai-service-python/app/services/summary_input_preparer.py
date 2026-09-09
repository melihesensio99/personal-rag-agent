import re


class SummaryInputPreparer:
    # Character budget, not a tokenizer-specific token limit. Includes scope markers.
    # About 10k tokens for typical Latin/Turkish prose. Full cleaned articles
    # below this size are sent intact; longer articles are section-compressed.
    MAX_MODEL_INPUT_CHARS = 40_000
    IMPORTANT = re.compile(
        r"abstract|summary|methods?|materials|results?|findings?|discussion|conclusions?|"
        r"limitations?|öz(et)?|yöntem|metot|bulgular|sonuç|tartışma|sınırlılıklar", re.I
    )
    HEADINGS = re.compile(
        r"^(?:abstract|summary|introduction|background|materials(?: and methods)?|"
        r"methods?|results?(?: and discussion)?|findings?|discussion|conclusions?|"
        r"limitations?|references|bibliography|acknowledg(?:e)?ments|"
        r"özet|giriş|yöntem(?:ler)?|bulgular|sonuç(?:lar)?|tartışma|sınırlılıklar|kaynakça)$", re.I
    )
    NOISE = re.compile(r"^(?:references|bibliography|acknowledg(?:e)?ments|kaynakça|teşekkür)$", re.I)

    @classmethod
    def prepare(cls, text: str, *, max_chars: int | None = None) -> str:
        budget = max_chars or cls.MAX_MODEL_INPUT_CHARS
        normalized = cls.normalize_whitespace(text)
        sections: list[tuple[str, str]] = []
        heading = ""
        lines: list[str] = []
        for line in normalized.splitlines():
            label = re.sub(r"^#{1,6}\s+", "", line).strip().rstrip(":")
            label = re.sub(r"^\d+(?:\.\d+)*[.)]?\s+", "", label)
            if line.startswith("#") or cls.HEADINGS.fullmatch(label):
                sections.append((heading, "\n".join(lines).strip()))
                heading, lines = label, []
            else:
                lines.append(line)
        sections.append((heading, "\n".join(lines).strip()))
        kept = [(h, b) for h, b in sections if (h or b) and not cls.NOISE.fullmatch(h)]
        # XML sources can emit an explicit section marker and the same title node.
        deduped: list[tuple[str, str]] = []
        for section in kept:
            if deduped and section[0].casefold() == deduped[-1][0].casefold() and not deduped[-1][1]:
                deduped[-1] = (deduped[-1][0], section[1])
            else:
                deduped.append(section)
        kept = deduped
        cleaned = "\n\n".join(cls._section(h, b) for h, b in kept)
        # Avoid turning unusual, entirely reference-like input into an empty request.
        if not cleaned:
            cleaned = normalized
            kept = [("", cleaned)]
        if len(cleaned) <= budget:
            return cleaned

        marker = "[CONTENT COMPRESSED FOR SUMMARY]\n[Selected excerpts; omitted text has not been evaluated.]\n"
        available = budget - len(marker)
        has_headings = any(h for h, _ in kept)
        if not has_headings:
            return marker + cls._excerpt(cleaned, available)

        rendered = [cls._section(h, b) for h, b in kept]
        # Allocate fairly across sections, with triple weight for scientific evidence.
        weights = [3 if cls.IMPORTANT.search(h) else 1 for h, _ in kept]
        allocations = [0] * len(kept)
        remaining = available - 2 * (len(kept) - 1)
        if remaining < len(kept) * 80:
            # Extremely fragmented documents: keep evidence sections and opening context.
            ranked = sorted(range(len(kept)), key=lambda i: (-weights[i], i))
            chosen = sorted(set([0] + ranked[:max(1, available // 200 - 1)]))
            rendered = [rendered[i] for i in chosen]
            weights = [weights[i] for i in chosen]
            allocations = [0] * len(chosen)
            remaining = available - 2 * (len(chosen) - 1)
        while remaining > 0:
            active = [i for i, value in enumerate(rendered) if allocations[i] < len(value)]
            if not active:
                break
            total_weight = sum(weights[i] for i in active)
            round_budget = remaining
            for i in active:
                take = min(remaining, len(rendered[i]) - allocations[i], max(1, round_budget * weights[i] // total_weight))
                allocations[i] += take
                remaining -= take
        return marker + "\n\n".join(cls._excerpt(value, size) for value, size in zip(rendered, allocations))

    @staticmethod
    def _section(heading: str, body: str) -> str:
        return f"## {heading}\n{body}".strip() if heading else body

    @staticmethod
    def _excerpt(text: str, budget: int) -> str:
        if len(text) <= budget:
            return text
        separator = "\n[… omitted …]\n"
        if budget < 3 * len(separator):
            return text[:budget]
        size = (budget - 2 * len(separator)) // 3
        middle = max(0, len(text) // 2 - size // 2)
        return separator.join((text[:size], text[middle:middle + size], text[-size:]))

    @staticmethod
    def normalize_whitespace(text: str) -> str:
        lines = [" ".join(line.split()) for line in text.splitlines()]
        condensed = re.sub(r"\n{3,}", "\n\n", "\n".join(lines)).strip()
        if not condensed:
            raise ValueError("Summary input cannot be empty.")
        return condensed
