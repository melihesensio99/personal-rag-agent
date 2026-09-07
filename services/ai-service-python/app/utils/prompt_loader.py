from pathlib import Path


class PromptLoader:
    def __init__(self, prompt_path: str) -> None:
        self._prompt_path = Path(prompt_path)

    def load(self) -> str:
        prompt = self._prompt_path.read_text(encoding="utf-8").strip()
        if not prompt:
            raise ValueError(f"Prompt file is empty: {self._prompt_path}")

        return prompt
