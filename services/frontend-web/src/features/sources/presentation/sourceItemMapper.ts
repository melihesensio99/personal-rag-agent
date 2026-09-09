import type { Content } from '../../../domain/content/Content';
import type { SourceItem } from '../../../types';
import { FALLBACK_SOURCE_IMAGE } from '../../../shared/sourceImage';
import type { ContentChunk } from '../../../domain/content/ContentChunk';

export function mapContentToSourceItem(content: Content, chunks: ContentChunk[] = []): SourceItem {
  const isYouTube = content.source === 'youtube';
  const sourceUrl = content.originalUrl ?? content.rawText;
  const sortedChunks = [...chunks].sort((left, right) => left.index - right.index);
  const wordsCount = sortedChunks
    .reduce((total, chunk) => total + chunk.text.trim().split(/\s+/).filter(Boolean).length, 0);
  return {
    id: content.id,
    type: isYouTube ? 'youtube' : 'web',
    title: content.title,
    originalUrl: sourceUrl,
    duration: isYouTube ? 'Video' : 'Web makalesi',
    reliability: 100,
    synthesisNumber: `#${content.id.slice(0, 8).toUpperCase()}`,
    category: content.tags.join(' & ').toLocaleUpperCase('tr') || 'GENEL ARAŞTIRMA',
    heroImage: content.imageUrl?.trim() || FALLBACK_SOURCE_IMAGE,
    readerBlocks: content.readerBlocks,
    author: { name: 'Hafıza', role: 'Kaynak tabanlı analiz', avatarUrl: '' },
    executiveSummary: splitSummary(content.summary),
    findings: content.keyPoints.slice(0, 6).map((point, index) => ({
      id: `${content.id}-${index}`,
      phase: `${String(index + 1).padStart(2, '0')} / BULGU`,
      timestamp: '[Kaynak]',
      timeSeconds: 0,
      ...splitFinding(point),
      confidence: 'Kaynak metninden sentezlendi',
    })),
    qaPairs: [],
    transcript: sortedChunks.map((chunk) => ({
      time: `Bölüm ${String(chunk.index + 1).padStart(2, '0')}`,
      seconds: chunk.index,
      speaker: isYouTube ? 'Video transkripti' : 'Makale metni',
      text: chunk.text,
      highlighted: chunk.index === 0,
    })),
    chunks: sortedChunks.map((chunk) => ({
      id: chunk.id,
      index: chunk.index,
      text: chunk.text,
      tokens: Math.ceil(chunk.text.length / 4),
      charStart: chunk.charStart,
      charEnd: chunk.charEnd,
      hasEmbedding: chunk.hasEmbedding,
      category: isYouTube ? 'Transkript' : 'Makale',
    })),
    telemetry: {
      chunksCount: sortedChunks.length,
      referencedNamesCount: 0,
      wordsCount: String(wordsCount),
      vectorDimensions: 0,
      matchPercentage: sortedChunks.length === 0
        ? 0
        : Math.round((sortedChunks.filter((chunk) => chunk.hasEmbedding).length / sortedChunks.length) * 100),
    },
    tags: content.tags,
    dateAdded: content.createdAt.toISOString().split('T')[0],
  };
}

function splitFinding(point: string): { title: string; description: string } {
  point = cleanGeneratedText(point);
  const separator = point.indexOf(':');
  if (separator > 0 && separator <= 100 && point.slice(separator + 1).trim()) {
    return { title: point.slice(0, separator).trim(), description: point.slice(separator + 1).trim() };
  }
  // Older summaries have no heading; show their original text without inventing one.
  return { title: '', description: point };
}

function splitSummary(summary: string): string[] {
  const lines = summary.split(/\n+/).map(line => cleanGeneratedText(line).replace(/^\s*[-•]\s*/, '').trim()).filter(Boolean);
  if (lines.length !== 1) return lines;
  // Make saved paragraph summaries readable too, without regenerating their content.
  const segmenter = new Intl.Segmenter('tr', { granularity: 'sentence' });
  return Array.from(segmenter.segment(lines[0]), part => part.segment.trim()).filter(Boolean);
}

function cleanGeneratedText(text: string): string {
  return text
    .replace(/\*\*([^*]+)\*\*/g, '$1')
    .replace(/__([^_]+)__/g, '$1')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/^\s*#{1,6}\s+/gm, '')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}
