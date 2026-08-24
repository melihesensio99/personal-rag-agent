class SummaryInputPreparer:
    MAX_MODEL_INPUT_CHARS = 18000
    SECTION_CHARS = 6000

    @classmethod
    def prepare(cls, text: str) -> str:
        normalized_text = cls.normalize_whitespace(text)

        if len(normalized_text) <= cls.MAX_MODEL_INPUT_CHARS:
            return normalized_text

        head = normalized_text[: cls.SECTION_CHARS].strip()
        middle_start = max((len(normalized_text) // 2) - (cls.SECTION_CHARS // 2), 0)
        middle = normalized_text[middle_start : middle_start + cls.SECTION_CHARS].strip()
        tail = normalized_text[-cls.SECTION_CHARS :].strip()

        return "\n\n".join(
            [
                "[CONTENT COMPRESSED FOR SUMMARY]",
                "[BEGINNING]",
                head,
                "[MIDDLE]",
                middle,
                "[END]",
                tail,
            ]
        )

    @staticmethod
    def normalize_whitespace(text: str) -> str:
        condensed = " ".join(text.split())
        if not condensed:
            raise ValueError("Summary input cannot be empty.")

        return condensed
