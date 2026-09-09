import React, { useState, useEffect } from 'react';
import {
  Play,
  Pause,
  RotateCcw,
  Volume2,
  Search,
  ExternalLink,
  Clock,
  Layers,
} from 'lucide-react';
import { SourceItem, NavigationTab } from '../types';

interface SourceDetailScreenProps {
  source: SourceItem;
  initialSeekSeconds?: number;
  onNavigate: (tab: NavigationTab) => void;
}

export const SourceDetailScreen: React.FC<SourceDetailScreenProps> = ({
  source,
  initialSeekSeconds = 0,
}) => {
  const [activeSubTab, setActiveSubTab] = useState<'transcript' | 'chunks'>('transcript');
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(initialSeekSeconds);
  const [transcriptSearch, setTranscriptSearch] = useState('');

  // Timer simulation for playback
  useEffect(() => {
    let timer: any;
    if (isPlaying) {
      timer = setInterval(() => {
        setCurrentTime((prev) => (prev >= 1458 ? 0 : prev + 1));
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [isPlaying]);

  useEffect(() => {
    if (initialSeekSeconds > 0) {
      setCurrentTime(initialSeekSeconds);
    }
  }, [initialSeekSeconds]);

  const formatSeconds = (sec: number) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const filteredTranscript = source.transcript.filter(
    (line) =>
      line.text.toLowerCase().includes(transcriptSearch.toLowerCase()) ||
      line.speaker.toLowerCase().includes(transcriptSearch.toLowerCase()) ||
      line.time.includes(transcriptSearch)
  );

  return (
    <div id="source-detail-screen" className="flex flex-col gap-6 w-full">
      {/* Top Banner Header */}
      <div className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl p-6 shadow-lg flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <img
            src={source.author.avatarUrl}
            alt={source.author.name}
            className="w-14 h-14 rounded-xl object-cover ring-2 ring-[#ffb77d]/30 shrink-0"
          />
          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <span className="font-mono text-[10px] uppercase text-[#ffb77d] font-bold">
                {source.category}
              </span>
              <span className="text-[#a38c7c] text-xs">•</span>
              <span className="font-mono text-[10px] text-[#dbc2b0]">
                {source.synthesisNumber}
              </span>
            </div>
            <h1 className="font-serif text-2xl text-[#e3e2e6] font-semibold">
              {source.title}
            </h1>
            <span className="font-sans text-xs text-[#a38c7c] mt-0.5">
              {source.author.name} — {source.author.role}
            </span>
          </div>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <a
            href={source.originalUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-1.5 px-3.5 py-2 bg-[#1f1f23] hover:bg-[#292a2d] text-[#dbc2b0] hover:text-[#e3e2e6] rounded-xl font-mono text-xs transition-colors border border-[#292a2d]"
          >
            <span>Orijinal Bağlantı</span>
            <ExternalLink className="w-3.5 h-3.5" />
          </a>
        </div>
      </div>

      {/* Simulated Audio/Video Scrubbing Player */}
      <div className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl p-5 shadow-md flex flex-col gap-3">
        <div className="flex items-center justify-between text-xs font-mono text-[#dbc2b0]">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-[#ffb77d] animate-pulse"></span>
            <span>Senkronize Oynatıcı & Transkript Eşleşmesi</span>
          </div>
          <div>
            <span className="text-[#ffb77d] font-bold">{formatSeconds(currentTime)}</span>
            <span className="text-[#a38c7c]"> / {source.duration}</span>
          </div>
        </div>

        {/* Scrubber Bar */}
        <div className="relative w-full py-1">
          <input
            type="range"
            min={0}
            max={1458}
            value={currentTime}
            onChange={(e) => setCurrentTime(Number(e.target.value))}
            className="w-full h-1.5 bg-[#292a2d] rounded-lg appearance-none cursor-pointer accent-[#ffb77d]"
          />
        </div>

        {/* Player Controls */}
        <div className="flex items-center justify-between pt-1">
          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsPlaying(!isPlaying)}
              className="w-9 h-9 rounded-full bg-[#ffb77d] text-[#4d2600] flex items-center justify-center hover:bg-[#ffdcc3] transition-all shadow-sm cursor-pointer"
            >
              {isPlaying ? <Pause className="w-4 h-4 fill-current" /> : <Play className="w-4 h-4 fill-current ml-0.5" />}
            </button>
            <button
              onClick={() => setCurrentTime(0)}
              title="Başa Sar"
              className="p-2 text-[#a38c7c] hover:text-[#e3e2e6] hover:bg-[#292a2d] rounded-lg transition-colors cursor-pointer"
            >
              <RotateCcw className="w-4 h-4" />
            </button>
            <span className="font-sans text-xs text-[#dbc2b0]/70 ml-2">
              {isPlaying ? 'Oynatılıyor (Transkript takip ediliyor)' : 'Duraklatıldı'}
            </span>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1.5 text-[#a38c7c] text-xs font-mono">
              <Volume2 className="w-4 h-4" />
              <span>100%</span>
            </div>
          </div>
        </div>
      </div>

      {/* Sub Tabs Selector */}
      <div className="flex items-center justify-between border-b border-[#292a2d] pb-2">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveSubTab('transcript')}
            className={`flex items-center gap-2 px-4 py-2 rounded-xl font-sans text-xs font-semibold transition-all cursor-pointer ${
              activeSubTab === 'transcript'
                ? 'bg-[#292a2d] text-[#ffb77d] border border-[#ffb77d]/30'
                : 'text-[#dbc2b0] hover:bg-[#1f1f23] hover:text-[#e3e2e6]'
            }`}
          >
            <Clock className="w-3.5 h-3.5" />
            <span>Zaman Damgalı Transkript</span>
          </button>
          <button
            onClick={() => setActiveSubTab('chunks')}
            className={`flex items-center gap-2 px-4 py-2 rounded-xl font-sans text-xs font-semibold transition-all cursor-pointer ${
              activeSubTab === 'chunks'
                ? 'bg-[#292a2d] text-[#ffb77d] border border-[#ffb77d]/30'
                : 'text-[#dbc2b0] hover:bg-[#1f1f23] hover:text-[#e3e2e6]'
            }`}
          >
            <Layers className="w-3.5 h-3.5" />
            <span>Vektör Chunk Dökümü ({source.chunks.length})</span>
          </button>
        </div>
      </div>

      {/* Tab 1: Timestamped Transcript */}
      {activeSubTab === 'transcript' && (
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between gap-3 bg-[#1b1b1f] p-3 rounded-xl border border-[#292a2d]">
            <div className="flex items-center gap-2 px-3 py-1.5 bg-[#0d0e11] rounded-lg border border-[#292a2d] flex-1 max-w-sm">
              <Search className="w-3.5 h-3.5 text-[#a38c7c]" />
              <input
                type="text"
                value={transcriptSearch}
                onChange={(e) => setTranscriptSearch(e.target.value)}
                placeholder="Transkript içinde ara..."
                className="bg-transparent font-sans text-xs text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none w-full"
              />
            </div>
            <span className="font-mono text-[11px] text-[#a38c7c]">
              {filteredTranscript.length} transkript segmenti
            </span>
          </div>

          <div className="flex flex-col gap-3">
            {filteredTranscript.map((line, idx) => {
              const isCurrent =
                currentTime >= line.seconds && currentTime < line.seconds + 200;
              return (
                <div
                  key={idx}
                  onClick={() => {
                    setCurrentTime(line.seconds);
                    setIsPlaying(true);
                  }}
                  className={`p-4 rounded-xl border transition-all cursor-pointer flex flex-col md:flex-row items-start gap-4 ${
                    isCurrent
                      ? 'bg-[#292a2d] border-[#ffb77d] shadow-md'
                      : line.highlighted
                      ? 'bg-[#1b1b1f] border-[#d97707]/40 hover:bg-[#1f1f23]'
                      : 'bg-[#1b1b1f] border-[#292a2d] hover:bg-[#1f1f23]'
                  }`}
                >
                  <div className="flex md:flex-col items-center md:items-start gap-2 shrink-0">
                    <span
                      className={`font-mono text-xs px-2 py-0.5 rounded font-semibold ${
                        isCurrent
                          ? 'bg-[#ffb77d] text-[#4d2600]'
                          : 'bg-[#0d0e11] text-[#ffb77d] border border-[#292a2d]'
                      }`}
                    >
                      [{line.time}]
                    </span>
                    <span className="font-sans text-[11px] text-[#a38c7c] truncate max-w-[120px]">
                      {line.speaker}
                    </span>
                  </div>

                  <div className="flex-1">
                    <p className="font-serif text-sm md:text-[15px] text-[#e3e2e6] leading-relaxed">
                      {line.text}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Tab 2: Vector Chunks Breakdown */}
      {activeSubTab === 'chunks' && (
        <div className="flex flex-col gap-4">
          <div className="bg-[#1b1b1f] p-4 rounded-xl border border-[#292a2d] flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Layers className="w-4 h-4 text-[#ffb77d]" />
              <span className="font-sans text-xs text-[#e3e2e6] font-semibold">
                Anlamsal Ayrıştırma Parametreleri
              </span>
            </div>
            <div className="flex items-center gap-4 font-mono text-[11px] text-[#a38c7c]">
              <span>Boyut: <strong className="text-[#ffb77d]">1536d</strong></span>
              <span>Model: <strong className="text-[#e3e2e6]">text-embedding-004</strong></span>
              <span>Chunking: <strong className="text-[#e3e2e6]">512 Tokens</strong></span>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {source.chunks.map((chunk) => (
              <div
                key={chunk.id}
                className="bg-[#1b1b1f] p-4 rounded-xl border border-[#292a2d] flex flex-col justify-between gap-3 shadow-sm hover:border-[#ffb77d]/40 transition-colors"
              >
                <div className="flex flex-col gap-2">
                  <div className="flex items-center justify-between">
                    <span className="font-mono text-[10px] text-[#ffb77d] bg-[#0d0e11] px-2 py-0.5 rounded border border-[#292a2d]">
                      {chunk.id}
                    </span>
                    <span className="font-mono text-[10px] text-[#dbc2b0] bg-[#292a2d] px-2 py-0.5 rounded">
                      {chunk.category}
                    </span>
                  </div>
                  <p className="font-mono text-xs text-[#e3e2e6] bg-[#0d0e11] p-3 rounded-lg border border-[#292a2d] leading-relaxed">
                    {chunk.text}
                  </p>
                </div>

                <div className="flex items-center justify-between font-mono text-[10px] text-[#a38c7c] border-t border-[#292a2d] pt-2">
                  <span>{chunk.tokens} Token</span>
                  <span className="text-[#ffb77d]">Kosinüs Benzerliği: {chunk.similarity}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
