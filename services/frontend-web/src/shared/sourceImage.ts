import type { SyntheticEvent } from 'react';

export const FALLBACK_SOURCE_IMAGE = '/assets/source-placeholder.svg';

export function useFallbackSourceImage(event: SyntheticEvent<HTMLImageElement>): void {
  const image = event.currentTarget;

  if (!image.src.endsWith(FALLBACK_SOURCE_IMAGE)) {
    image.src = FALLBACK_SOURCE_IMAGE;
  }
}
