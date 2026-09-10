import React, { useMemo, useState } from 'react';
import {
  BarChart3,
  Check,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Copy,
  ExternalLink,
  FileText,
  Globe2,
  Layers,
  MessageSquare,
  Quote,
  Search,
  Sparkles,
  Zap,
} from 'lucide-react';
import type { NavigationTab, SourceItem } from '../types';
import { FALLBACK_SOURCE_IMAGE, useFallbackSourceImage } from '../shared/sourceImage';

type DetailTab = 'summary' | 'original';

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
  const [showChunksDrawer, setShowChunksDrawer] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [copyError, setCopyError] = useState<string | null>(null);

  const normalizedSearch = searchQuery.trim().toLocaleLowerCase('tr');

  const filteredFindings = useMemo(() => {
    return source.findings.filter((f) => {
      if (!normalizedSearch) return true;
      const text = `${f.title || ''} ${f.description || ''} ${f.phase || ''}`.toLocaleLowerCase('tr');
      return text.includes(normalizedSearch);
    });
  }, [normalizedSearch, source.findings]);

  const filteredChunks = useMemo(() => {
    return source.chunks.filter((c) => {
      if (!normalizedSearch) return true;
      return c.text.toLocaleLowerCase('tr').includes(normalizedSearch);
    });
  }, [normalizedSearch, source.chunks]);

  const isIndexed = source.chunks.length > 0 && source.chunks.every((chunk) => chunk.hasEmbedding);

  const handleCopyText = async (id: string, text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopyError(null);
      setCopiedId(id);
      setTimeout(() => setCopiedId(null), 2000);
    } catch {
      setCopyError('Kopyalama yapılamadı. Metni seçerek kopyalayabilirsin.');
    }
  };

  return (
    <div id="source-detail-screen" className="flex w-full flex-col gap-6">
      {/* Immersive Top Header */}
      <header className="relative overflow-hidden rounded-2xl border border-[#34353a] bg-gradient-to-r from-[#1e1f25] via-[#17181d] to-[#121316] p-6 shadow-2xl">
        <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-[#ffb77d]/5 blur-3xl" />
        <div className="relative z-10 flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
          <div className="flex min-w-0 items-start gap-5">
            <div className="relative shrink-0 overflow-hidden rounded-2xl ring-2 ring-[#ffb77d]/30 shadow-xl">
              <img
                src={source.heroImage || FALLBACK_SOURCE_IMAGE}
                onError={useFallbackSourceImage}
                alt={source.title}
                className="h-24 w-32 object-cover transition-transform duration-300 hover:scale-105"
              />
              <span className="absolute bottom-2 left-2 rounded-md bg-[#000000]/80 px-2 py-0.5 font-mono text-[9px] font-bold tracking-wider text-[#ffb77d] backdrop-blur-md">
                {source.type === 'youtube' ? 'YOUTUBE' : 'MAKALE'}
              </span>
            </div>
            <div className="min-w-0">
              <div className="mb-1.5 flex flex-wrap items-center gap-2 font-mono text-[11px] font-semibold text-[#ffb77d]">
                <span className="rounded-md bg-[#ffb77d]/15 px-2 py-0.5">{source.synthesisNumber}</span>
                <span className="text-[#a38c7c]">•</span>
                <span className="text-[#c5b5a8]">{source.category}</span>
                <span className="text-[#a38c7c]">•</span>
                <span className="text-[#a38c7c]">{source.dateAdded}</span>
              </div>
              <h1 className="font-serif text-2xl font-bold leading-snug text-[#f3f2f6] md:text-3xl">
                {source.title}
              </h1>
              <div className="mt-3 flex items-center gap-2 font-mono text-[11px] text-[#a38c7c]">
                <CheckCircle2 className={`h-4 w-4 ${isIndexed ? 'text-emerald-400' : 'text-[#ffb77d]'}`} />
                <span className="font-medium text-[#c5b5a8]">
                  {isIndexed ? 'Vektör veri tabanında indekslendi • Sorulara hazır' : 'Sentezleme ve vektörleştirme sürüyor'}
                </span>
              </div>
            </div>
          </div>

          <button
            onClick={() => onNavigate('ai-asistan', source.id)}
            className="group flex shrink-0 items-center justify-center gap-2.5 rounded-xl border border-[#ffb77d]/40 bg-gradient-to-r from-[#ffb77d]/20 to-[#ffb77d]/10 px-5 py-3 text-xs font-bold text-[#ffb77d] shadow-lg transition-all duration-200 hover:border-[#ffb77d]/60 hover:bg-[#ffb77d]/30 hover:shadow-[#ffb77d]/10"
          >
            <Sparkles className="h-4 w-4 transition-transform duration-200 group-hover:scale-110" />
            <span>AI Asistanına Sor</span>
          </button>
        </div>

        {/* Telemetry Stats Bar */}
        <div className="mt-6 grid grid-cols-2 gap-3 border-t border-[#2d2e33] pt-4 sm:grid-cols-4">
          <div className="flex items-center gap-3 rounded-xl border border-[#2d2e33]/60 bg-[#141518]/70 px-3.5 py-2.5">
            <Layers className="h-4 w-4 text-[#ffb77d]" />
            <div>
              <div className="text-[10px] font-medium uppercase tracking-wider text-[#938275]">Vektör Bölümü</div>
              <div className="font-mono text-xs font-semibold text-[#e3e2e6]">{source.telemetry.chunksCount} Parça</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#2d2e33]/60 bg-[#141518]/70 px-3.5 py-2.5">
            <FileText className="h-4 w-4 text-[#ffb77d]" />
            <div>
              <div className="text-[10px] font-medium uppercase tracking-wider text-[#938275]">Kelime Sayısı</div>
              <div className="font-mono text-xs font-semibold text-[#e3e2e6]">{source.telemetry.wordsCount} Kelime</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#2d2e33]/60 bg-[#141518]/70 px-3.5 py-2.5">
            <Zap className="h-4 w-4 text-[#ffb77d]" />
            <div>
              <div className="text-[10px] font-medium uppercase tracking-wider text-[#938275]">İndeksleme</div>
              <div className="font-mono text-xs font-semibold text-[#e3e2e6]">%{source.telemetry.embeddingCoveragePercentage} Embedding hazır</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#2d2e33]/60 bg-[#141518]/70 px-3.5 py-2.5">
            <BarChart3 className="h-4 w-4 text-[#ffb77d]" />
            <div>
              <div className="text-[10px] font-medium uppercase tracking-wider text-[#938275]">Analiz Türü</div>
              <div className="font-mono text-xs font-semibold text-emerald-400">AI özeti</div>
            </div>
          </div>
        </div>
      </header>

      {isLoading && (
        <div className="rounded-xl border border-[#ffb77d]/30 bg-[#ffb77d]/10 px-4 py-3 font-mono text-xs text-[#ffb77d] animate-pulse">
          Kaynak içeriği ve derin sentez bulguları işleniyor...
        </div>
      )}
      {error && <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">{error}</div>}
      {copyError && <p role="status" className="text-sm text-amber-200">{copyError}</p>}

      {/* Navigation Bar */}
      <nav className="flex items-center justify-between border-b border-[#292a2d] pb-3">
        <div className="flex items-center gap-2">
          <TabButton active={activeTab === 'summary'} onClick={() => setActiveTab('summary')} icon={<Sparkles className="h-3.5 w-3.5 text-[#ffb77d]" />}>
            Özet ve bulgular
          </TabButton>
          <TabButton active={activeTab === 'original'} onClick={() => setActiveTab('original')} icon={<Globe2 className="h-3.5 w-3.5 text-[#a38c7c]" />}>
            Orijinal Kaynak Metni
          </TabButton>
        </div>

        {/* Global Search in Details */}
        {activeTab === 'summary' && (
          <div className="hidden items-center gap-2 rounded-xl border border-[#2d2e33] bg-[#141518] px-3.5 py-1.5 sm:flex w-72">
            <Search className="h-3.5 w-3.5 text-[#a38c7c]" />
            <input
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Bulgularda ve metinlerde ara..."
              className="w-full bg-transparent text-xs text-[#e3e2e6] outline-none placeholder:text-[#938275]"
            />
          </div>
        )}
      </nav>

      {/* SUMMARY & DEEP FINDINGS TAB */}
      {activeTab === 'summary' && (
        <div className="flex flex-col gap-8">
          {/* Executive Summary Card */}
          <section className="relative overflow-hidden rounded-2xl border border-[#2e2f34] bg-[#17181c] p-6 shadow-xl">
            <div className="mb-4 flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-[#ffb77d]/15 border border-[#ffb77d]/30 text-[#ffb77d]">
                  <Quote className="h-3.5 w-3.5" />
                </div>
                <h2 className="font-serif text-xl font-bold text-[#f3f2f6]">Bir bakışta</h2>
              </div>
              <button
                onClick={() => handleCopyText('summary', [source.title, ...source.executiveSummary.map(point => `• ${point}`), ...source.findings.map(finding => [finding.title, finding.description].filter(Boolean).join(': '))].join('\n\n'))}
                className="flex items-center gap-2 rounded-lg border border-[#34353a] px-3 py-2 text-xs text-[#dbc2b0] hover:text-[#ffb77d]"
              >
                {copiedId === 'summary' ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                {copiedId === 'summary' ? 'Kopyalandı' : 'Özeti kopyala'}
              </button>
            </div>

            {/* High Impact Highlight Box */}
            <div className="border-y border-[#2d2e33] bg-[#101114]/55 px-3 py-1 md:px-6">
              {source.executiveSummary.length > 0 ? (
                <ul className="divide-y divide-[#2d2e33] text-base leading-7 text-[#e3d7cd]">
                  {source.executiveSummary.map((point, index) => (
                    <li key={index}><OverviewPoint text={point} index={index} /></li>
                  ))}
                </ul>
              ) : <p className="text-base text-[#e3d7cd]">Bu kaynak için henüz özet oluşturulmadı.</p>}
            </div>

            {/* Tags Showcase */}
            {source.tags.length > 0 && (
              <div className="mt-5 flex flex-wrap items-center gap-2 border-t border-[#25262b] pt-4">
                <span className="font-mono text-[10px] font-bold text-[#938275] uppercase tracking-wider mr-1">Konu Başlıkları:</span>
                {source.tags.map((tag) => (
                  <span
                    key={tag}
                    className="rounded-lg border border-[#ffb77d]/25 bg-[#ffb77d]/10 px-3 py-1 font-mono text-[11px] font-medium text-[#ffb77d] transition-all hover:bg-[#ffb77d]/20 hover:border-[#ffb77d]/40"
                  >
                    #{tag}
                  </span>
                ))}
              </div>
            )}
          </section>

          {/* Deep Findings Grid Section */}
          <section className="flex flex-col gap-5">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center justify-between gap-4">
                  <h2 className="font-serif text-2xl font-bold text-[#f3f2f6]">Ayrıntılı bulgular</h2>
                  <span aria-live="polite" className="shrink-0 rounded-full bg-[#ffb77d]/15 border border-[#ffb77d]/30 px-2.5 py-0.5 font-mono text-xs font-bold text-[#ffb77d]">
                    {filteredFindings.length} Çıkarım
                  </span>
                </div>
                <p className="mt-1 text-xs text-[#a38c7c]">
                  Her madde tek bir noktayı açıklar. Yöntem, sonuç ve sınırlılıkları birlikte değerlendirin.
                </p>
              </div>

              {/* Mobile Search input */}
              <div className="flex items-center gap-2 rounded-xl border border-[#2d2e33] bg-[#141518] px-3.5 py-2 sm:hidden">
                <Search className="h-3.5 w-3.5 text-[#a38c7c]" />
                <input
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Bulgularda ara..."
                  className="w-full bg-transparent text-xs text-[#e3e2e6] outline-none placeholder:text-[#938275]"
                />
              </div>
            </div>

            {filteredFindings.length > 0 ? (
              <div className="divide-y divide-[#2d2e33] border-y border-[#2d2e33]">
                {filteredFindings.map((finding, index) => {

                  return (
                    <article
                      key={finding.id}
                      className="group relative flex flex-col justify-between overflow-hidden bg-[#16171b]/35 py-7 pl-16 pr-2 transition-all duration-300 hover:bg-[#1d1e23] md:pl-24 md:pr-5"
                    >
                      <span aria-hidden="true" className="absolute left-2 top-6 font-serif text-5xl font-bold leading-none text-[#ffb77d]/20 transition-colors group-hover:text-[#ffb77d]/50 md:left-5 md:text-6xl">
                        {String(index + 1).padStart(2, '0')}
                      </span>
                      {/* Top Meta Bar */}
                      <div>
                        <div className="flex items-center justify-between pb-2">
                          <div className="flex items-center gap-2 font-mono text-[11px]">
                            <span className="text-[#a38c7c]">{finding.confidence || 'Sentezlenmiş'}</span>
                          </div>

                          <div className="flex items-center gap-1 opacity-70 transition-opacity group-hover:opacity-100">
                            <button
                              onClick={() => handleCopyText(finding.id, [finding.title, finding.description].filter(Boolean).join(': '))}
                              title="Bulguyu Kopyala"
                              className="flex h-7 w-7 items-center justify-center rounded-lg border border-[#2d2e33] bg-[#101114] text-[#a38c7c] transition-colors hover:border-[#ffb77d]/30 hover:text-[#ffb77d]"
                            >
                              {copiedId === finding.id ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
                            </button>
                          </div>
                        </div>

                        {/* Finding Content */}
                        <div className="mt-2 max-w-4xl">
                          <h3 className="font-serif text-xl font-bold text-[#f3f2f6] leading-snug group-hover:text-[#ffb77d] transition-colors md:text-2xl">
                            {finding.title || `Bulgı ${index + 1}`}
                          </h3>
                          <p className="mt-3 whitespace-pre-line text-[15px] leading-8 text-[#d1c2b5] md:text-base">
                            {finding.description}
                          </p>
                        </div>
                      </div>

                      {/* Interactive Bottom Actions */}
                      <div className="mt-5 border-t border-[#25262a] pt-3">
                        <div className="flex items-center justify-between">
                          <button
                            onClick={() => setActiveTab('original')}
                            className="text-xs font-semibold text-[#c5b5a8] hover:text-[#ffb77d]"
                          >
                            Kaynağı oku
                          </button>

                          <button
                            onClick={() => onNavigate('ai-asistan', source.id)}
                            className="flex items-center gap-1 font-mono text-[11px] font-semibold text-[#ffb77d] hover:underline"
                          >
                            <MessageSquare className="h-3 w-3" />
                            <span>AI'ya Sor</span>
                          </button>
                        </div>


                      </div>
                    </article>
                  );
                })}
              </div>
            ) : (
              <EmptyState message={normalizedSearch ? 'Aramanızla eşleşen bir bulgu bulunamadı.' : 'Bu kaynak için henüz detaylı bulgu oluşturulmadı.'} />
            )}
          </section>

          {/* Integrated Source Passages Accordion / Drawer */}
          <section className="overflow-hidden rounded-2xl border border-[#2e2f34] bg-[#17181c] p-6 shadow-xl">
            <div className="flex items-center justify-between cursor-pointer select-none" onClick={() => setShowChunksDrawer(!showChunksDrawer)}>
              <div className="flex items-center gap-3">
                <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-[#ffb77d]/15 border border-[#ffb77d]/30 text-[#ffb77d]">
                  <FileText className="h-4 w-4" />
                </div>
                <div>
                  <h3 className="font-serif text-lg font-bold text-[#f3f2f6]">Analiz Edilen Tüm Kaynak Kesitleri</h3>
                  <p className="text-xs text-[#a38c7c]">Toplanan transkript / makale parçaları ({source.chunks.length} Bölüm)</p>
                </div>
              </div>
              <button className="flex items-center gap-1.5 rounded-xl border border-[#2d2e33] bg-[#101114] px-4 py-2 font-mono text-xs font-semibold text-[#ffb77d] hover:bg-[#1f2026] transition-colors">
                <span>{showChunksDrawer ? 'Kapat' : 'Tüm Kesitleri İncele'}</span>
                {showChunksDrawer ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
              </button>
            </div>

            {showChunksDrawer && (
              <div className="mt-6 flex flex-col gap-4 border-t border-[#25262a] pt-5">
                {filteredChunks.length > 0 ? (
                  filteredChunks.map((chunk) => (
                    <div key={chunk.id} className="rounded-xl border border-[#292a2e] bg-[#101114] p-4">
                      <div className="mb-2 flex items-center justify-between font-mono text-[11px] text-[#ffb77d]">
                        <span>Bölüm #{chunk.index + 1} ({chunk.category})</span>
                        <span>{chunk.tokens} jeton • Vektörlü: {chunk.hasEmbedding ? 'Evet' : 'Hayır'}</span>
                      </div>
                      <p className="font-sans text-sm leading-7 text-[#d5c7ba]">{chunk.text}</p>
                    </div>
                  ))
                ) : (
                  <p className="text-center font-mono text-xs text-[#a38c7c] py-4">Bölüm metni bulunamadı.</p>
                )}
              </div>
            )}
          </section>
        </div>
      )}

      {/* ORIGINAL SOURCE TAB */}
      {activeTab === 'original' && (
        <section className="overflow-hidden rounded-2xl border border-[#292a2d] bg-[#1b1b1f]">
          <div className="flex flex-col gap-3 border-b border-[#292a2d] p-4 md:flex-row md:items-center md:justify-between">
            <div className="min-w-0">
              <h2 className="font-serif text-lg font-semibold text-[#e3e2e6]">Orijinal Web Sayfası</h2>
              <p className="mt-1 truncate font-mono text-[10px] text-[#a38c7c]">{source.originalUrl}</p>
            </div>
            <a
              href={source.originalUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex shrink-0 items-center justify-center gap-1.5 rounded-xl bg-[#ffb77d] px-4 py-2.5 text-xs font-bold text-[#4d2600] transition-colors hover:bg-[#ffdcc3]"
            >
              Yeni Sekmede Aç <ExternalLink className="h-3.5 w-3.5" />
            </a>
          </div>
          <div className="border-b border-[#292a2d] bg-[#0d0e11] px-4 py-2 font-mono text-[10px] text-[#a38c7c]">
            Site güvenlik nedeniyle gömülü görünümü engellerse yukarıdaki butonu kullanabilirsin.
          </div>
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

function TabButton({
  active,
  onClick,
  icon,
  children,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      className={`flex shrink-0 items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold transition-all duration-200 ${
        active
          ? 'border border-[#ffb77d]/40 bg-[#26272c] text-[#ffb77d] shadow-md'
          : 'text-[#c5b5a8] hover:bg-[#1a1b1e] hover:text-[#e3e2e6]'
      }`}
    >
      {icon}
      {children}
    </button>
  );
}

function EmptyState({ message }: { message: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-[#343538] bg-[#1b1b1f] px-6 py-12 text-center">
      <Sparkles className="mx-auto h-8 w-8 text-[#a38c7c]" />
      <p className="mt-3 text-sm text-[#dbc2b0]">{message}</p>
    </div>
  );
}

function OverviewPoint({ text, index }: { text: string; index: number }) {
  const separator = text.indexOf(':');
  const hasLabel = separator > 0 && separator < 45 && Boolean(text.slice(separator + 1).trim());
  const label = hasLabel ? text.slice(0, separator).trim() : `Öne çıkan nokta ${index + 1}`;
  const body = hasLabel ? text.slice(separator + 1).trim() : text;
  return (
    <div className="grid gap-2 py-5 md:grid-cols-[220px_1fr] md:gap-8 md:py-6">
      <p className="flex items-start gap-2 font-mono text-xs font-bold uppercase tracking-[0.16em] text-[#ffb77d] md:pt-1 md:text-sm">
        <span aria-hidden="true" className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-[#ffb77d]" />{label}
      </p>
      <p className="max-w-4xl whitespace-pre-line text-[15px] leading-8 text-[#e3d7cd] md:text-base">{body}</p>
    </div>
  );
}
