from __future__ import annotations

from collections.abc import Callable
from typing import TypeVar


T = TypeVar("T")


def run_with_retries(
    operation: Callable[[str | None], T],
    repair_hint_builder: Callable[[Exception], str],
    *,
    max_attempts: int,
    failure_message: str,
    retryable_errors: tuple[type[Exception], ...] = (Exception,),
) -> T:
    """Run a structured-output operation and retry it with validation feedback."""
    last_error: Exception | None = None
    repair_hint: str | None = None

    for _ in range(max_attempts):
        try:
            return operation(repair_hint)
        except retryable_errors as error:
            last_error = error
            repair_hint = repair_hint_builder(error)

    raise RuntimeError(failure_message) from last_error
