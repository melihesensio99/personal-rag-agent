import React, { useState } from 'react';
import {
  Search,
  Filter,
  BookOpen,
  Calendar,
  Clock,
  ShieldCheck,
  ArrowUpRight,
  Plus,
  Layers,
  Sparkles,
  ExternalLink,
  BrainCircuit,
  Trash2,
} from 'lucide-react';
import { SourceItem, NavigationTab } from '../types';
import { FALLBACK_SOURCE_IMAGE, useFallbackSourceImage } from '../shared/sourceImage';

interface SourcesScreenProps {
  sources: SourceItem[];
  onSelectSource: (sourceId: string) => void;
  onNavigate: (tab: NavigationTab, targetSourceId?: string) => void;
  onDeleteSource: (sourceId: string) => Promise<void>;
}

export const SourcesScreen: React.FC<SourcesScreenProps> = ({
  sources,
  onSelectSource,
  onNavigate,
  onDeleteSource,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState<'all' | 'web' | 'youtube'>('all');
  const sourceTypeFilters = [
    { value: 'all', label: 'Tümü' },
    { value: 'web', label: 'Makaleler' },
    { value: 'youtube', label: 'YouTube' },
  ] as const;

  const filteredSources = sources.filter((source) => {
    const matchesSearch =
      source.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      source.author.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      source.category.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesType = selectedType === 'all' || source.type === selectedType;

    return matchesSearch && matchesType;
  });

  return (
    <div id="sources-screen" className="flex flex-col gap-6 w-full">
      {/* Top Banner & Metric Header */}
      <div className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl p-6 shadow-md flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl md:text-3xl text-[#e3e2e6] font-semibold">
            Bilgi Havuzu & Kaynak Arşivi
          </h1>
          <p className="font-sans text-xs text-[#dbc2b0]/70 mt-1">
            İncelenen, vektörleştirilen ve anlamsal özetleri çıkarılan araştırma dokümanları ve video brifingleri.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <div className="bg-[#1f1f23] px-3 py-2 rounded-xl border border-[#292a2d] flex flex-col">
            <span className="font-mono text-[10px] text-[#a38c7c] uppercase">Arşiv Kaydı</span>
            <span className="font-mono text-sm font-bold text-[#ffb77d]">{sources.length} Doküman</span>
          </div>
          <button
            onClick={() => onNavigate('hizli-yakala')}
            className="flex items-center gap-2 px-4 py-2.5 bg-[#ffb77d] text-[#4d2600] rounded-xl font-sans text-xs font-bold hover:bg-[#ffdcc3] transition-all cursor-pointer shadow-sm"
          >
            <Plus className="w-4 h-4" />
            <span>Yeni Kaynak Yakala</span>
          </button>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 bg-[#1b1b1f] p-3 rounded-xl border border-[#292a2d]">
        <div className="flex items-center gap-2 px-3 py-1.5 bg-[#0d0e11] rounded-lg border border-[#292a2d] flex-1 max-w-md">
          <Search className="w-4 h-4 text-[#a38c7c]" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Başlık, yazar veya kavram ara..."
            className="bg-transparent font-sans text-xs text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none w-full"
          />
        </div>

        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0">
          {sourceTypeFilters.map((filter) => (
            <button
              key={filter.value}
              onClick={() => setSelectedType(filter.value)}
              className={`px-3 py-1 rounded-lg font-sans text-xs transition-colors whitespace-nowrap cursor-pointer ${
                selectedType === filter.value
                  ? 'bg-[#292a2d] text-[#ffb77d] font-semibold border border-[#ffb77d]/30'
                  : 'text-[#dbc2b0] hover:bg-[#1f1f23] hover:text-[#e3e2e6]'
              }`}
            >
              {filter.label}
            </button>
          ))}
        </div>
      </div>

      {/* Sources Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {filteredSources.map((source) => (
          <div
            key={source.id}
            className="bg-[#1b1b1f] rounded-2xl overflow-hidden border border-[#292a2d] shadow-lg hover:border-[#ffb77d]/40 transition-all flex flex-col justify-between group"
          >
            <div>
              {/* Header Image with Overlays */}
              <div className="relative h-48 w-full bg-[#0d0e11] overflow-hidden">
                <img
                  src={source.heroImage || FALLBACK_SOURCE_IMAGE}
                  onError={useFallbackSourceImage}
                  alt={source.title}
                  className="w-full h-full object-cover opacity-85 group-hover:scale-105 transition-transform duration-500"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-[#1b1b1f] via-transparent to-black/40"></div>
                <div className="absolute top-3 left-3 flex items-center gap-2">
                  <span className="bg-[#0d0e11]/80 backdrop-blur-xs px-2 py-0.5 rounded font-mono text-[10px] text-[#ffb77d] border border-[#292a2d] uppercase">
                    {source.type === 'youtube' ? 'YouTube' : 'Makale'}
                  </span>
                  <span className="bg-[#0d0e11]/80 backdrop-blur-xs px-2 py-0.5 rounded font-mono text-[10px] text-[#e3e2e6] border border-[#292a2d]">
                    {source.duration}
                  </span>
                </div>
                <div className="absolute top-3 right-3 flex items-center gap-1 font-mono text-[10px] bg-[#0d0e11]/80 px-2 py-0.5 rounded text-[#ffb77d] border border-[#292a2d]">
                  <ShieldCheck className="w-3 h-3 text-[#d97707]" />
                  <span>%{source.reliability}</span>
                </div>
              </div>

              {/* Content Body */}
              <div className="p-5 flex flex-col gap-3">
                <div className="flex flex-col gap-1">
                  <span className="font-mono text-[10px] uppercase text-[#ffb77d] tracking-wider font-semibold">
                    {source.category}
                  </span>
                  <h3 className="font-serif text-xl text-[#e3e2e6] font-semibold group-hover:text-[#ffb77d] transition-colors leading-snug">
                    {source.title}
                  </h3>
                </div>

                <p className="font-sans text-xs text-[#dbc2b0]/70 line-clamp-2 leading-relaxed">
                  {source.executiveSummary[0]}
                </p>

                {/* Author row */}
                <div className="flex items-center gap-2.5 pt-2 border-t border-[#292a2d]">
                  <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-[#ffb77d]/35 bg-[#ffb77d]/10 text-[#ffb77d]" title="Hafıza">
                    <BrainCircuit className="h-4 w-4" />
                  </div>
                  <div className="flex flex-col min-w-0">
                    <span className="font-sans text-xs font-semibold text-[#e3e2e6] truncate">
                      {source.author.name}
                    </span>
                    <span className="font-mono text-[10px] text-[#a38c7c] truncate">
                      {source.author.role}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Footer Action Buttons */}
            <div className="px-5 pb-5 pt-1 flex items-center justify-between gap-2 border-t border-[#292a2d]/60 mt-2">
              <button
                onClick={() => {
                  onSelectSource(source.id);
                  onNavigate('kaynak-detayi', source.id);
                }}
                className="px-3.5 py-1.5 bg-[#1f1f23] hover:bg-[#292a2d] text-[#e3e2e6] hover:text-[#ffb77d] rounded-lg font-sans text-xs font-medium transition-colors cursor-pointer border border-[#292a2d]"
              >
                Kaynağı İncele
              </button>

              <button
                onClick={async () => {
                  if (!window.confirm(`“${source.title}” kaynağı kalıcı olarak silinsin mi?`)) return;
                  try {
                    await onDeleteSource(source.id);
                  } catch (error) {
                    window.alert(error instanceof Error ? error.message : 'Kaynak silinemedi.');
                  }
                }}
                title="Kaynağı sil"
                aria-label={`${source.title} kaynağını sil`}
                className="flex h-8 w-8 items-center justify-center rounded-lg border border-red-500/25 text-red-300/75 transition-colors hover:border-red-400/60 hover:bg-red-500/10 hover:text-red-200"
              >
                <Trash2 className="h-4 w-4" />
              </button>

              <button
                id={`btn-ask-ai-${source.id}`}
                onClick={() => onNavigate('ai-asistan', source.id)}
                title={`Bu kaynak için AI Asistan'da soru sor: ${source.title}`}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-[#ffb77d]/15 hover:bg-[#ffb77d]/25 text-[#ffb77d] border border-[#ffb77d]/30 rounded-lg font-sans text-xs font-semibold transition-all cursor-pointer shadow-xs group/btn"
              >
                <Sparkles className="w-3.5 h-3.5 group-hover/btn:scale-110 transition-transform" />
                <span>AI'ya Sor</span>
              </button>
            </div>
          </div>
        ))}
      </div>
      {filteredSources.length === 0 && (
        <div className="rounded-2xl border border-dashed border-[#343538] bg-[#1b1b1f] px-6 py-14 text-center">
          <BookOpen className="mx-auto h-8 w-8 text-[#a38c7c]"/>
          <h2 className="mt-4 font-serif text-xl text-[#e3e2e6]">{sources.length === 0 ? 'Arşivin henüz boş' : 'Eşleşen kaynak bulunamadı'}</h2>
          <p className="mt-2 text-sm text-[#a38c7c]">{sources.length === 0 ? 'Eklediğin gerçek kaynaklar burada görünecek.' : 'Arama veya etiket filtresini değiştirmeyi dene.'}</p>
        </div>
      )}
    </div>
  );
};
