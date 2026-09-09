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
  const [expandedFindingId, setExpandedFindingId] = useState<string | null>(null);
  const [showChunksDrawer, setShowChunksDrawer] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);

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

  const handleCopyText = (id: string, text: string) => {
    void navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
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
              <div className="font-mono text-xs font-semibold text-[#e3e2e6]">%{source.telemetry.matchPercentage} Tamamlandı</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#2d2e33]/60 bg-[#141518]/70 px-3.5 py-2.5">
            <BarChart3 className="h-4 w-4 text-[#ffb77d]" />
            <div>
              <div className="text-[10px] font-medium uppercase tracking-wider text-[#938275]">Güven Skoru</div>
              <div className="font-mono text-xs font-semibold text-emerald-400">%{source.reliability} Doğrulanmış</div>
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

      {/* Navigation Bar */}
      <nav className="flex items-center justify-between border-b border-[#292a2d] pb-3">
        <div className="flex items-center gap-2">
          <TabButton active={activeTab === 'summary'} onClick={() => setActiveTab('summary')} icon={<Sparkles className="h-3.5 w-3.5 text-[#ffb77d]" />}>
            Sentez & Derin Bulgular
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
                <h2 className="font-serif text-xl font-bold text-[#f3f2f6]">Sentez Özeti & Ana Çıkarım</h2>
              </div>
              <span className="font-mono text-[10px] font-semibold text-[#a38c7c]">AI ÖZETİ</span>
            </div>

            {/* High Impact Highlight Box */}
            <div className="relative rounded-xl border-l-4 border-[#ffb77d] bg-[#101114] p-5 shadow-inner">
              <p className="font-serif text-[16px] leading-8 text-[#e3d7cd] tracking-wide">
                {source.executiveSummary[0] || 'Bu kaynak için henüz özet oluşturulmadı.'}
              </p>
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
                <div className="flex items-center gap-2">
                  <h2 className="font-serif text-2xl font-bold text-[#f3f2f6]">Detaylı Önemli Bulgular</h2>
                  <span className="rounded-full bg-[#ffb77d]/15 border border-[#ffb77d]/30 px-2.5 py-0.5 font-mono text-xs font-bold text-[#ffb77d]">
                    {filteredFindings.length} Çıkarım
                  </span>
                </div>
                <p className="mt-1 text-xs text-[#a38c7c]">
                  Kaynaktan yapay zeka tarafından çıkarılan kritik bulgular ve derinlikli analizler.
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
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                {filteredFindings.map((finding, index) => {
                  const isExpanded = expandedFindingId === finding.id;
                  const matchingChunk = source.chunks[index % (source.chunks.length || 1)];

                  return (
                    <article
                      key={finding.id}
                      className="group relative flex flex-col justify-between overflow-hidden rounded-2xl border border-[#2d2e33] bg-[#16171b] p-5 shadow-lg transition-all duration-300 hover:border-[#ffb77d]/40 hover:bg-[#1a1b20]"
                    >
                      {/* Top Meta Bar */}
                      <div>
                        <div className="flex items-center justify-between border-b border-[#25262a] pb-3">
                          <div className="flex items-center gap-2 font-mono text-[11px]">
                            <span className="font-bold text-[#ffb77d]">{finding.phase || `${String(index + 1).padStart(2, '0')} / BULGU`}</span>
                            <span className="text-[#3b3d45]">|</span>
                            <span className="text-[#a38c7c]">{finding.confidence || 'Sentezlenmiş'}</span>
                          </div>

                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleCopyText(finding.id, finding.description)}
                              title="Bulguyu Kopyala"
                              className="flex h-7 w-7 items-center justify-center rounded-lg border border-[#2d2e33] bg-[#101114] text-[#a38c7c] transition-colors hover:border-[#ffb77d]/30 hover:text-[#ffb77d]"
                            >
                              {copiedId === finding.id ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
                            </button>
                          </div>
                        </div>

                        {/* Finding Content */}
                        <div className="mt-3.5">
                          <h3 className="font-serif text-base font-bold text-[#f3f2f6] leading-snug group-hover:text-[#ffb77d] transition-colors">
                            {finding.title || `Bulgu #${index + 1}`}
                          </h3>
                          <p className="mt-2 text-sm leading-7 text-[#d1c2b5]">
                            {finding.description}
                          </p>
                        </div>
                      </div>

                      {/* Interactive Bottom Actions */}
                      <div className="mt-4 border-t border-[#25262a] pt-3">
                        <div className="flex items-center justify-between">
                          <button
                            onClick={() => setExpandedFindingId(isExpanded ? null : finding.id)}
                            className="flex items-center gap-1.5 font-mono text-[11px] font-semibold text-[#a38c7c] hover:text-[#ffb77d] transition-colors"
                          >
                            <span>{isExpanded ? 'Kaynak Metnini Gizle' : 'İlgili Kaynak Metni'}</span>
                            {isExpanded ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
                          </button>

                          <button
                            onClick={() => onNavigate('ai-asistan', source.id)}
                            className="flex items-center gap-1 font-mono text-[11px] font-semibold text-[#ffb77d] hover:underline"
                          >
                            <MessageSquare className="h-3 w-3" />
                            <span>AI'ya Sor</span>
                          </button>
                        </div>

                        {/* Accordion / Expanded Chunk text */}
                        {isExpanded && matchingChunk && (
                          <div className="mt-3 rounded-xl border border-[#ffb77d]/20 bg-[#101114] p-3.5 font-mono text-xs leading-6 text-[#bdaea1]">
                            <div className="mb-1.5 flex items-center justify-between text-[10px] text-[#ffb77d]">
                              <span>BÖLÜM #{matchingChunk.index + 1} KESİTİ</span>
                              <span>{matchingChunk.tokens} jeton</span>
                            </div>
                            <p className="font-sans text-xs italic leading-6 text-[#c2b2a4]">"{matchingChunk.text}"</p>
                          </div>
                        )}
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
