import React, { useMemo, useState } from 'react';
import {
  BookOpen,
  CheckCircle2,
  ExternalLink,
  FileText,
  Globe2,
  Search,
  Sparkles,
} from 'lucide-react';
import type { NavigationTab, SourceItem } from '../types';
import { FALLBACK_SOURCE_IMAGE, useFallbackSourceImage } from '../shared/sourceImage';

type DetailTab = 'summary' | 'reader' | 'original';

interface SourceDetailScreenProps {
  source: SourceItem;
  isLoading?: boolean;
  error?: string | null;
  initialSeekSeconds?: number;
  onNavigate: (tab: NavigationTab, targetSourceId?: string) => void;
}

export const SourceDetailScreen: React.FC<SourceDetailScreenProps> = ({
  source,
  isLoading = false,
  error,
  onNavigate,
}) => {
  const [activeTab, setActiveTab] = useState<DetailTab>('summary');
  const [searchQuery, setSearchQuery] = useState('');
  const normalizedSearch = searchQuery.trim().toLocaleLowerCase('tr');

  const readableBlocks = useMemo(() => {
    const blocks = source.readerBlocks.length > 0
      ? source.readerBlocks
      : source.transcript.map((section) => ({ type: 'paragraph' as const, text: section.text }));
    return blocks.filter((block) => {
      const searchableText = `${block.text ?? ''} ${block.caption ?? ''}`.toLocaleLowerCase('tr');
      return !normalizedSearch || searchableText.includes(normalizedSearch);
    });
  }, [normalizedSearch, source.readerBlocks, source.transcript]);
  const isIndexed = source.chunks.length > 0 && source.chunks.every((chunk) => chunk.hasEmbedding);

  return (
    <div id="source-detail-screen" className="flex w-full flex-col gap-6">
      <header className="flex flex-col gap-4 rounded-2xl border border-[#292a2d] bg-[#1b1b1f] p-5 shadow-lg md:flex-row md:items-center md:justify-between">
        <div className="flex min-w-0 items-center gap-4">
          <img
            src={source.heroImage || FALLBACK_SOURCE_IMAGE}
            onError={useFallbackSourceImage}
            alt={source.title}
            className="h-20 w-28 shrink-0 rounded-xl object-cover ring-1 ring-[#ffb77d]/30"
          />
          <div className="min-w-0">
            <div className="mb-1 flex items-center gap-2 font-mono text-[10px] uppercase text-[#ffb77d]">
              <span>{source.type === 'youtube' ? 'YouTube' : 'Makale'}</span>
              <span className="text-[#a38c7c]">•</span>
              <span>{source.synthesisNumber}</span>
            </div>
            <h1 className="font-serif text-2xl font-semibold leading-tight text-[#e3e2e6]">{source.title}</h1>
            <div className="mt-2 flex items-center gap-1.5 font-mono text-[10px] text-[#a38c7c]">
              <CheckCircle2 className={`h-3.5 w-3.5 ${isIndexed ? 'text-emerald-400' : 'text-[#ffb77d]'}`} />
              <span>{isIndexed ? 'Kaynak indekslendi ve AI aramasına hazır' : 'Kaynak işleniyor'}</span>
            </div>
          </div>
        </div>
        <button
          onClick={() => onNavigate('ai-asistan', source.id)}
          className="flex shrink-0 items-center justify-center gap-1.5 rounded-xl border border-[#ffb77d]/30 bg-[#ffb77d]/15 px-4 py-2.5 text-xs font-semibold text-[#ffb77d] transition-colors hover:bg-[#ffb77d]/25"
        >
          <Sparkles className="h-3.5 w-3.5" /> Bu Kaynağa Sor
        </button>
      </header>

      {isLoading && <div className="rounded-xl border border-[#ffb77d]/25 bg-[#ffb77d]/10 px-4 py-3 font-mono text-xs text-[#ffb77d]">Kaynak içeriği hazırlanıyor...</div>}
      {error && <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">{error}</div>}

      <nav className="flex items-center gap-2 overflow-x-auto border-b border-[#292a2d] pb-3">
        <TabButton active={activeTab === 'summary'} onClick={() => setActiveTab('summary')} icon={<Sparkles className="h-3.5 w-3.5" />}>Özet ve Bulgular</TabButton>
        <TabButton active={activeTab === 'reader'} onClick={() => setActiveTab('reader')} icon={<BookOpen className="h-3.5 w-3.5" />}>Okuma Modu</TabButton>
        <TabButton active={activeTab === 'original'} onClick={() => setActiveTab('original')} icon={<Globe2 className="h-3.5 w-3.5" />}>Orijinal Kaynak</TabButton>
      </nav>

      {activeTab === 'summary' && (
        <div className="flex flex-col gap-5">
          <section className="rounded-2xl border border-[#292a2d] bg-[#1b1b1f] p-6">
            <div className="mb-4 flex items-center gap-2">
              <FileText className="h-4 w-4 text-[#ffb77d]" />
              <h2 className="font-serif text-xl font-semibold text-[#e3e2e6]">Sentez Özeti</h2>
            </div>
            <p className="font-serif text-[15px] leading-8 text-[#dbc2b0]">{source.executiveSummary[0] || 'Bu kaynak için henüz özet oluşturulmadı.'}</p>
            <div className="mt-5 flex flex-wrap gap-2">
              {source.tags.map((tag) => <span key={tag} className="rounded-lg border border-[#ffb77d]/20 bg-[#ffb77d]/10 px-2.5 py-1 font-mono text-[10px] text-[#ffb77d]">{tag}</span>)}
            </div>
          </section>

          <section className="rounded-2xl border border-[#292a2d] bg-[#1b1b1f] p-6">
            <h2 className="mb-4 font-serif text-xl font-semibold text-[#e3e2e6]">Önemli Bulgular</h2>
            {source.findings.length > 0 ? (
              <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                {source.findings.map((finding, index) => (
                  <article key={finding.id} className="rounded-xl border border-[#292a2d] bg-[#0d0e11] p-4">
                    <span className="font-mono text-[10px] text-[#ffb77d]">{String(index + 1).padStart(2, '0')} / BULGU</span>
                    <p className="mt-2 text-sm leading-6 text-[#dbc2b0]">{finding.description}</p>
                  </article>
                ))}
              </div>
            ) : <EmptyState message="Bu kaynak için henüz önemli bulgu oluşturulmadı." />}
          </section>
        </div>
      )}

      {activeTab === 'reader' && (
        <section className="flex flex-col gap-4">
          <div className="flex flex-col gap-3 rounded-xl border border-[#292a2d] bg-[#1b1b1f] p-4 md:flex-row md:items-center md:justify-between">
            <div>
              <h2 className="font-serif text-lg font-semibold text-[#e3e2e6]">{source.type === 'youtube' ? 'Transkript Okuma Modu' : 'Makale Okuma Modu'}</h2>
              <p className="mt-1 text-xs text-[#a38c7c]">Dikkat dağıtan sayfa öğeleri olmadan temiz kaynak metni.</p>
            </div>
            <div className="flex items-center gap-2 rounded-lg border border-[#292a2d] bg-[#0d0e11] px-3 py-2 md:w-80">
              <Search className="h-3.5 w-3.5 text-[#a38c7c]" />
              <input value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} placeholder="Makale içinde ara..." className="w-full bg-transparent text-xs text-[#e3e2e6] outline-none placeholder:text-[#a38c7c]/50" />
            </div>
          </div>

          {readableBlocks.length > 0 && (
            <article className="rounded-2xl border border-[#ded5ca] bg-[#f4efe8] px-6 py-8 text-[#201b18] shadow-lg md:px-12 md:py-10">
              <div className="mx-auto max-w-3xl">
                {readableBlocks.map((block, index) => (
                  <ReaderBlockView key={`${block.type}-${index}`} block={block} title={source.title} />
                ))}
              </div>
            </article>
          )}
          {!isLoading && readableBlocks.length === 0 && <EmptyState message={normalizedSearch ? 'Aramayla eşleşen bir bölüm bulunamadı.' : 'Bu kaynak için okunabilir metin bulunamadı.'} />}
        </section>
      )}

      {activeTab === 'original' && (
        <section className="overflow-hidden rounded-2xl border border-[#292a2d] bg-[#1b1b1f]">
          <div className="flex flex-col gap-3 border-b border-[#292a2d] p-4 md:flex-row md:items-center md:justify-between">
            <div className="min-w-0">
              <h2 className="font-serif text-lg font-semibold text-[#e3e2e6]">Orijinal Web Sayfası</h2>
              <p className="mt-1 truncate font-mono text-[10px] text-[#a38c7c]">{source.originalUrl}</p>
            </div>
            <a href={source.originalUrl} target="_blank" rel="noopener noreferrer" className="flex shrink-0 items-center justify-center gap-1.5 rounded-xl bg-[#ffb77d] px-4 py-2.5 text-xs font-bold text-[#4d2600] transition-colors hover:bg-[#ffdcc3]">
              Yeni Sekmede Aç <ExternalLink className="h-3.5 w-3.5" />
            </a>
          </div>
          <div className="border-b border-[#292a2d] bg-[#0d0e11] px-4 py-2 font-mono text-[10px] text-[#a38c7c]">Site güvenlik nedeniyle gömülü görünümü engellerse yukarıdaki butonu kullanabilirsin.</div>
          <iframe
            title={`Orijinal kaynak: ${source.title}`}
            src={source.originalUrl}
            className="h-[70vh] w-full bg-white"
            sandbox="allow-scripts allow-same-origin allow-popups"
            referrerPolicy="no-referrer"
          />
        </section>
      )}
    </div>
  );
};

function TabButton({ active, onClick, icon, children }: { active: boolean; onClick: () => void; icon: React.ReactNode; children: React.ReactNode }) {
  return <button onClick={onClick} className={`flex shrink-0 items-center gap-2 rounded-xl px-4 py-2 text-xs font-semibold transition-colors ${active ? 'border border-[#ffb77d]/30 bg-[#292a2d] text-[#ffb77d]' : 'text-[#dbc2b0] hover:bg-[#1f1f23]'}`}>{icon}{children}</button>;
}

function EmptyState({ message }: { message: string }) {
  return <div className="rounded-2xl border border-dashed border-[#343538] bg-[#1b1b1f] px-6 py-12 text-center"><BookOpen className="mx-auto h-7 w-7 text-[#a38c7c]" /><p className="mt-3 text-sm text-[#dbc2b0]">{message}</p></div>;
}

function ReaderBlockView({ block, title }: { block: SourceItem['readerBlocks'][number]; title: string }) {
  if (block.type === 'image' && block.url) {
    return (
      <figure className="my-8">
        <img src={block.url} onError={useFallbackSourceImage} alt={block.caption || title} className="max-h-[520px] w-full rounded-xl object-contain bg-[#201b18]" />
        {block.caption && <figcaption className="mt-2 text-center font-sans text-xs italic text-[#6e625a]">{block.caption}</figcaption>}
      </figure>
    );
  }

  if (block.type === 'heading' && block.text) {
    return block.level === 1
      ? <h2 className="mb-6 mt-2 font-serif text-3xl font-bold leading-tight">{block.text}</h2>
      : <h3 className="mb-3 mt-8 font-serif text-xl font-bold leading-snug text-[#422c1d]">{block.text}</h3>;
  }

  if (block.type === 'paragraph' && block.text) {
    return <p className="mb-5 whitespace-pre-wrap font-serif text-base leading-8 text-[#302924]">{block.text}</p>;
  }

  return null;
}
