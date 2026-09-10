import React, { useState } from 'react';
import {
  Tv,
  FileText,
  Radar,
  Check,
  RefreshCw,
  Hourglass,
  Play,
  ExternalLink,
  ShieldCheck,
  ArrowRight,
  Sparkles,
  Clock,
  Send,
} from 'lucide-react';
import { SourceItem, NavigationTab, QAPair } from '../types';
import { FALLBACK_SOURCE_IMAGE, useFallbackSourceImage } from '../shared/sourceImage';
import { askSemanticQuestion } from '../app/dependencies';

interface QuickCaptureScreenProps {
  currentSource?: SourceItem;
  onNavigate: (tab: NavigationTab, targetSourceId?: string, seekSeconds?: number) => void;
  onAddNewSource: (url: string, type: 'youtube' | 'web') => Promise<SourceItem>;
}

export const QuickCaptureScreen: React.FC<QuickCaptureScreenProps> = ({
  currentSource,
  onNavigate,
  onAddNewSource,
}) => {
  const [activeInputType, setActiveInputType] = useState<'youtube' | 'web'>('youtube');
  const [urlInput, setUrlInput] = useState('');
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analysisProgress, setAnalysisProgress] = useState(0);
  const [analysisError, setAnalysisError] = useState<string | null>(null);
  const [hasResult, setHasResult] = useState(false);

  // Live Q&A state for the right sidebar
  const [qaThread, setQaThread] = useState<QAPair[]>(currentSource?.qaPairs ?? []);
  const [capturedSource, setCapturedSource] = useState<SourceItem>();
  const displayedSource = capturedSource ?? currentSource;
  const [questionInput, setQuestionInput] = useState('');
  const [isAsking, setIsAsking] = useState(false);

  // Play modal state
  const [isVideoModalOpen, setIsVideoModalOpen] = useState(false);

  const handleTabToggle = (type: 'youtube' | 'web') => {
    setActiveInputType(type);
    setUrlInput('');
    setAnalysisError(null);
    setAnalysisProgress(0);
    setHasResult(false);
    setCapturedSource(undefined);
  };

  const handleStartAnalysis = async () => {
    if (!urlInput.trim()) return;
    setAnalysisError(null);
    setIsAnalyzing(true);
    setAnalysisProgress(15);
    const progressTimer = window.setInterval(() => setAnalysisProgress((value) => Math.min(value + 8, 88)), 450);
    try {
      const createdSource = await onAddNewSource(urlInput, activeInputType);
      setCapturedSource(createdSource);
      setQaThread(createdSource.qaPairs);
      setAnalysisProgress(100);
      setHasResult(true);
    } catch (reason) {
      setAnalysisProgress(0);
      setAnalysisError(reason instanceof Error ? reason.message : 'Kaynak analiz edilemedi.');
    } finally {
      window.clearInterval(progressTimer);
      setIsAnalyzing(false);
    }
  };

  const handleAskQuestion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!questionInput.trim() || isAsking) return;

    const userQuestion = questionInput.trim();
    setQuestionInput('');
    setIsAsking(true);

    try {
      const response = await askSemanticQuestion(userQuestion, displayedSource?.id);
      const firstSource = response.sources[0];
      const newQA: QAPair = {
        id: `qa-${Date.now()}`,
        question: userQuestion,
        timeAgo: 'Şimdi',
        answer: response.answer,
        citation: firstSource
          ? `${firstSource.contentTitle} — Chunk ${firstSource.chunkIndex + 1}`
          : 'Kaynak kanıtı bulunamadı',
        citationTimestamp: firstSource ? `Chunk ${firstSource.chunkIndex + 1}` : '',
      };

      setQaThread((prev) => [newQA, ...prev]);
    } catch (error) {
      const newQA: QAPair = {
        id: `qa-${Date.now()}`,
        question: userQuestion,
        timeAgo: 'Şimdi',
        answer: error instanceof Error ? error.message : 'Soru cevaplanamadı. Lütfen tekrar deneyin.',
        citation: '—',
        citationTimestamp: '',
      };
      setQaThread((prev) => [newQA, ...prev]);
    } finally {
      setIsAsking(false);
    }
  };

  return (
    <div id="quick-capture-screen" className="flex flex-col w-full gap-8">
      {/* Top Section: Command Center Capture Composer */}
      <section
        id="composer-section"
        className="bg-[#1b1b1f] rounded-2xl p-6 shadow-xl relative overflow-hidden border border-[#292a2d]"
      >
        <div className="absolute -right-16 -top-16 w-80 h-80 bg-[#d97707]/10 rounded-full blur-3xl pointer-events-none"></div>

        <div className="flex flex-col gap-4 relative z-10">
          {/* Composer Header / Tab Switcher */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-1.5 bg-[#0d0e11] p-1 rounded-xl border border-[#292a2d]">
              <button
                id="tab-yt"
                onClick={() => handleTabToggle('youtube')}
                className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg font-sans text-xs font-semibold transition-all cursor-pointer ${
                  activeInputType === 'youtube'
                    ? 'bg-[#292a2d] text-[#ffb77d] shadow-sm border border-[#ffb77d]/30'
                    : 'text-[#dbc2b0] hover:text-[#e3e2e6] hover:bg-[#1f1f23]'
                }`}
              >
                <Tv className="w-3.5 h-3.5" />
                <span>YouTube Videosu</span>
              </button>
              <button
                id="tab-web"
                onClick={() => handleTabToggle('web')}
                className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg font-sans text-xs font-semibold transition-all cursor-pointer ${
                  activeInputType === 'web'
                    ? 'bg-[#292a2d] text-[#ffb77d] shadow-sm border border-[#ffb77d]/30'
                    : 'text-[#dbc2b0] hover:text-[#e3e2e6] hover:bg-[#1f1f23]'
                }`}
              >
                <FileText className="w-3.5 h-3.5" />
                <span>Makale / Web Bağlantısı</span>
              </button>
            </div>

            <div className="flex items-center gap-3">
              <span className="font-mono text-[11px] text-[#ffb68e] flex items-center gap-1.5">
                <span className="w-1.5 h-1.5 rounded-full bg-[#d97707] animate-pulse"></span>
                Sistem Hazır
              </span>
            </div>
          </div>

          {/* Single Unified Input Bar */}
          <div className="flex flex-col md:flex-row items-stretch gap-2 bg-[#0d0e11] p-1.5 rounded-xl border border-[#292a2d] shadow-inner">
            <div className="flex items-center gap-3 px-3 flex-1 min-w-0">
              <span className="font-mono text-xs text-[#ffb77d] font-bold select-none">&gt;</span>
              <input
                id="source-url-input"
                type="url"
                value={urlInput}
                onChange={(e) => setUrlInput(e.target.value)}
                placeholder={
                  activeInputType === 'youtube'
                    ? "YouTube video URL'si yapıştırın (ör. https://youtube.com/watch?v=...)"
                    : 'Makale veya web sayfası bağlantısı yapıştırın (ör. https://arxiv.org/abs/...)'
                }
                className="bg-transparent font-sans text-sm text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none w-full"
              />
            </div>
            <div className="flex items-center gap-1 shrink-0">
              <button
                id="analyze-submit-btn"
                onClick={handleStartAnalysis}
                disabled={isAnalyzing}
                className="flex items-center gap-2 px-5 py-2.5 bg-[#ffb77d] text-[#4d2600] rounded-lg font-sans text-xs font-bold hover:bg-[#ffdcc3] transition-all shadow-md active:scale-95 cursor-pointer disabled:opacity-50"
              >
                <Radar className={`w-4 h-4 ${isAnalyzing ? 'animate-spin' : ''}`} />
                <span>{isAnalyzing ? 'Analiz Ediliyor...' : 'Analiz Et ve Kaydet'}</span>
              </button>
            </div>
          </div>

          {analysisError && (
            <div role="alert" className="rounded-lg border border-red-400/30 bg-red-950/30 px-3 py-2 text-xs text-red-200">
              {analysisError}
            </div>
          )}

          {/* Live Analysis Pipeline Tracker */}
          {(isAnalyzing || hasResult) && <div className="pt-1">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2.5">
              {/* Step 1: Complete */}
              <div className="flex items-center gap-3 bg-[#1f1f23] p-3 rounded-lg border border-[#292a2d]">
                <div className="w-6 h-6 rounded-full bg-[#d97707]/20 text-[#ffb77d] flex items-center justify-center shrink-0">
                  <Check className="w-3.5 h-3.5" />
                </div>
                <div className="flex flex-col min-w-0">
                  <span className="font-mono text-[10px] text-[#e3e2e6] font-semibold truncate">
                    01. Bağlantı Çözümlendi
                  </span>
                  <span className="font-mono text-[10px] text-[#ffb77d] truncate">
                    Tamamlandı (0.4s)
                  </span>
                </div>
              </div>

              {/* Step 2: Complete */}
              <div className="flex items-center gap-3 bg-[#1f1f23] p-3 rounded-lg border border-[#292a2d]">
                <div className="w-6 h-6 rounded-full bg-[#d97707]/20 text-[#ffb77d] flex items-center justify-center shrink-0">
                  <Check className="w-3.5 h-3.5" />
                </div>
                <div className="flex flex-col min-w-0">
                  <span className="font-mono text-[10px] text-[#e3e2e6] font-semibold truncate">
                    02. Transkript Ayrıştırıldı
                  </span>
                  <span className="font-mono text-[10px] text-[#ffb77d] truncate">
                    Tamamlandı ({displayedSource?.telemetry.wordsCount ?? 0} kelime)
                  </span>
                </div>
              </div>

              {/* Step 3: In Progress or Complete */}
              <div
                className={`flex items-center gap-3 p-3 rounded-lg border ${
                  analysisProgress >= 100
                    ? 'bg-[#1f1f23] border-[#292a2d]'
                    : 'bg-[#292a2d] border-[#0053db]/40 shadow-sm'
                }`}
              >
                <div
                  className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 ${
                    analysisProgress >= 100
                      ? 'bg-[#d97707]/20 text-[#ffb77d]'
                      : 'bg-[#0053db]/30 text-[#b4c5ff]'
                  }`}
                >
                  {analysisProgress >= 100 ? (
                    <Check className="w-3.5 h-3.5" />
                  ) : (
                    <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  )}
                </div>
                <div className="flex flex-col min-w-0">
                  <span
                    className={`font-mono text-[10px] font-semibold truncate ${
                      analysisProgress >= 100 ? 'text-[#e3e2e6]' : 'text-[#b4c5ff]'
                    }`}
                  >
                    03. Anlamsal Sentez
                  </span>
                  <span
                    className={`font-mono text-[10px] truncate ${
                      analysisProgress >= 100 ? 'text-[#ffb77d]' : 'text-[#b4c5ff]/80'
                    }`}
                  >
                    {analysisProgress >= 100 ? 'Tamamlandı' : `İşleniyor (%${analysisProgress})`}
                  </span>
                </div>
              </div>

              {/* Step 4: Pending or Complete */}
              <div
                className={`flex items-center gap-3 p-3 rounded-lg border ${
                  analysisProgress >= 100
                    ? 'bg-[#1f1f23] border-[#292a2d]'
                    : 'bg-[#1f1f23]/60 border-[#292a2d]/60 opacity-75'
                }`}
              >
                <div
                  className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 ${
                    analysisProgress >= 100
                      ? 'bg-[#d97707]/20 text-[#ffb77d]'
                      : 'bg-[#343538] text-[#a38c7c]'
                  }`}
                >
                  {analysisProgress >= 100 ? (
                    <Check className="w-3.5 h-3.5" />
                  ) : (
                    <Hourglass className="w-3.5 h-3.5" />
                  )}
                </div>
                <div className="flex flex-col min-w-0">
                  <span className="font-mono text-[10px] text-[#dbc2b0] font-semibold truncate">
                    04. Hafıza İndeksi
                  </span>
                  <span className="font-mono text-[10px] text-[#a38c7c] truncate">
                    {analysisProgress >= 100 ? 'İndekslendi (48 chunk)' : 'Bekliyor'}
                  </span>
                </div>
              </div>
            </div>
          </div>}
        </div>
      </section>

      {/* Main Body: Two-Column Dossier Workspace with Warm Canvas */}
      {hasResult && displayedSource && <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 xl:gap-8 items-start">
        {/* Left / Center Editorial Dossier (Parchment Paper Canvas) */}
        <article
          id="editorial-dossier-card"
          className="lg:col-span-7 xl:col-span-8 bg-[#FAF7F2] text-[#1f1f23] rounded-2xl p-6 md:p-8 xl:p-9 shadow-2xl relative border border-[#E4DFD7]"
        >
          {/* Archival Stamp & Metadata Band */}
          <div className="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-[#E4DFD7] text-[#554336]">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="bg-[#1f1f23] text-[#FAF7F2] px-2 py-0.5 rounded text-[10px] font-mono font-semibold tracking-wider uppercase">
                {displayedSource.type === 'youtube' ? 'YOUTUBE BRİFİNGİ' : 'DÖKÜMAN BRİFİNGİ'}
              </span>
              <span className="text-xs text-[#a38c7c]">•</span>
              <span className="font-mono text-[11px] font-medium text-[#554336]">
                Süre: {displayedSource.duration}
              </span>
              <span className="text-xs text-[#a38c7c]">•</span>
              <span className="font-mono text-[11px] font-medium text-[#432100]">
                {displayedSource.telemetry.embeddingCoveragePercentage == null
                  ? 'İndeks durumu detay ekranında görüntülenebilir'
                  : `Embedding kapsamı: %${displayedSource.telemetry.embeddingCoveragePercentage}`}
              </span>
            </div>

            <div className="flex items-center gap-1.5 font-mono text-[10px] text-[#763300] font-semibold">
              <ShieldCheck className="w-3.5 h-3.5 text-[#d97707]" />
              <span>Sentez No: {displayedSource.synthesisNumber}</span>
            </div>
          </div>

          {/* Dossier Hero Media + Expressive Typography */}
          <div className="pt-6 flex flex-col gap-5">
            {/* Hero Image / Video thumbnail preview */}
            <div
              id="dossier-hero-media"
              onClick={() => setIsVideoModalOpen(true)}
              className="relative w-full h-64 md:h-72 rounded-xl overflow-hidden bg-[#1f1f23] cursor-pointer group shadow-md"
            >
              <img
                alt={displayedSource.title}
                className="w-full h-full object-cover opacity-90 transition-transform duration-500 group-hover:scale-105"
                src={displayedSource.heroImage || FALLBACK_SOURCE_IMAGE}
                onError={useFallbackSourceImage}
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#1f1f23] via-[#1f1f23]/40 to-transparent flex items-end p-5">
                <div className="flex items-center gap-3">
                  <span className="w-10 h-10 rounded-full bg-[#ffb77d] flex items-center justify-center text-[#4d2600] group-hover:bg-[#ffdcc3] group-hover:scale-110 transition-all shadow-lg">
                    <Play className="w-5 h-5 fill-current ml-0.5" />
                  </span>
                  <div className="flex flex-col">
                    <span className="font-mono text-[11px] text-[#FAF7F2] tracking-wider uppercase font-semibold">
                      Orijinal Kaynak: {displayedSource.originalUrl.replace('https://', '')}
                    </span>
                    <span className="font-sans text-[11px] text-[#ffb77d] opacity-90">
                      Önizlemeyi başlatmak için tıklayın
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Titles */}
            <div className="flex flex-col gap-1.5 pt-1">
              <span className="font-mono text-xs uppercase tracking-widest text-[#763300] font-bold">
                {displayedSource.category}
              </span>
              <h1 className="font-serif text-3xl md:text-4xl text-[#18181B] tracking-tight leading-snug font-semibold">
                {displayedSource.title}
              </h1>
            </div>

            {/* Executive Synthesis Block (Parchment Box) */}
            <div className="flex flex-col gap-3 bg-[#F4EFEB] p-6 rounded-xl border-l-4 border-[#d97707]">
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs uppercase tracking-widest text-[#432100] font-bold">
                  YÖNETİCİ SENTEZİ
                </span>
                <span className="font-mono text-[11px] text-[#a38c7c]">
                  3 Bölümlü Özümseme
                </span>
              </div>
              <div className="divide-y divide-[#E4DFD7] text-[#2f3034]">
                {displayedSource.executiveSummary.map((para, index) => {
                  const separator = para.indexOf(':');
                  const hasLabel = separator > 0 && separator < 70;
                  const label = hasLabel ? para.slice(0, separator).trim() : `Özet ${index + 1}`;
                  const body = hasLabel ? para.slice(separator + 1).trim() : para;
                  return (
                    <div key={index} className="grid gap-2 py-4 first:pt-0 last:pb-0 md:grid-cols-[190px_1fr] md:gap-6">
                      <h3 className="font-mono text-xs font-bold uppercase tracking-[0.14em] text-[#763300] md:pt-1">
                        {label}
                      </h3>
                      <p className="whitespace-pre-line font-serif text-[17px] leading-relaxed">{body}</p>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* 4 Structured Key Findings */}
            <div className="flex flex-col gap-4 pt-4">
              <div className="flex items-center justify-between">
                <h2 className="font-serif text-2xl text-[#18181B] font-semibold">
                  Kritik Bulgular & Zaman İmleri
                </h2>
                <span className="font-mono text-xs text-[#763300] font-medium">
                  {displayedSource.findings.length} Tespit Ayrıştırıldı
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {displayedSource.findings.map((finding) => (
                  <div
                    key={finding.id}
                    className="bg-white p-4 rounded-xl shadow-xs border border-[#E4DFD7] flex flex-col justify-between gap-3 hover:shadow-md hover:border-[#d97707]/50 transition-all"
                  >
                    <div className="flex flex-col gap-1.5">
                      <div className="flex items-center justify-between">
                        <span className="font-mono text-[11px] font-bold text-[#d97707]">
                          {finding.phase}
                        </span>
                        <button
                          onClick={() =>
                            onNavigate('kaynak-detayi', displayedSource.id, finding.timeSeconds)
                          }
                          title="Bu zaman damgasına git"
                          className="font-mono text-[10px] bg-[#F4EFEB] text-[#432100] px-2 py-0.5 rounded hover:bg-[#d97707] hover:text-white transition-colors cursor-pointer"
                        >
                          {finding.timestamp}
                        </button>
                      </div>
                      <h3 className="font-sans font-bold text-sm text-[#18181B]">
                        {finding.title}
                      </h3>
                      <p className="font-sans text-xs text-[#343538] leading-relaxed">
                        {finding.description}
                      </p>
                    </div>
                    <div className="text-[10px] text-[#a38c7c] font-mono border-t border-[#F4EFEB] pt-2">
                      {finding.confidence}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Dossier Action Bar */}
            <div className="flex items-center justify-end pt-6 border-t border-[#E4DFD7] mt-2">
              <a
                href={displayedSource.originalUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1.5 text-[#554336] hover:text-[#18181B] font-mono text-xs transition-colors"
              >
                <span>Orijinal Kaynağı Ziyaret Et</span>
                <ExternalLink className="w-3.5 h-3.5" />
              </a>
            </div>
          </div>
        </article>

        {/* Right Side: Grounded Contextual Cockpit Mini-Q&A Preview */}
        <aside className="lg:col-span-5 xl:col-span-4 flex flex-col gap-6">
          {/* Mini-Cockpit Card: "Bu Kaynağa Sor" */}
          <div
            id="qa-cockpit-card"
            className="bg-[#1b1b1f] p-6 rounded-2xl flex flex-col gap-4 shadow-lg border border-[#292a2d]"
          >
            <div className="flex items-center justify-between pb-1">
              <div className="flex items-center gap-2 text-[#ffb77d]">
                <Sparkles className="w-4 h-4" />
                <h2 className="font-sans font-bold text-sm text-[#e3e2e6]">Bu Kaynağa Sor</h2>
              </div>
              <span className="font-mono text-[10px] bg-[#1f1f23] border border-[#292a2d] px-2 py-0.5 rounded text-[#ffb77d]">
                Canlı Bağlam
              </span>
            </div>

            <p className="font-sans text-xs text-[#dbc2b0]/80 leading-relaxed">
              İncelenen bu video içeriği vektör uzayına bağlandı. Sorular yalnızca orijinal transkript ve zaman kodları referans alınarak yanıtlanır.
            </p>

            {/* Q&A Thread */}
            <div className="flex flex-col gap-3 max-h-[340px] overflow-y-auto pr-1">
              {qaThread.map((item) => (
                <div
                  key={item.id}
                  className="flex flex-col gap-2 bg-[#1f1f23] p-3.5 rounded-xl border border-[#292a2d]"
                >
                  {/* Question */}
                  <div className="flex items-start gap-2">
                    <span className="w-5 h-5 rounded-full bg-[#292a2d] text-[#ffb77d] flex items-center justify-center font-mono text-[10px] font-bold shrink-0 mt-0.5">
                      S
                    </span>
                    <div className="flex flex-col min-w-0">
                      <span className="font-sans text-xs font-semibold text-[#e3e2e6]">
                        {item.question}
                      </span>
                      <span className="font-mono text-[9px] text-[#a38c7c]">
                        Araştırmacı sorgusu • {item.timeAgo}
                      </span>
                    </div>
                  </div>

                  {/* Answer with Grounding Citation */}
                  <div className="flex items-start gap-2 pt-1 bg-[#0d0e11] p-3 rounded-lg border border-[#292a2d]">
                    <span className="w-5 h-5 rounded-full bg-[#d97707] text-[#432100] flex items-center justify-center font-mono text-[10px] font-bold shrink-0 mt-0.5">
                      C
                    </span>
                    <div className="flex flex-col gap-2">
                      <p className="font-sans text-xs text-[#e3e2e6] leading-relaxed">
                        {item.answer}
                      </p>
                      <div className="flex items-center gap-2 pt-1 border-t border-[#1f1f23]">
                        <span className="font-mono text-[10px] text-[#a38c7c]">Dayanak:</span>
                        <button
                          onClick={() => onNavigate('kaynak-detayi', displayedSource.id)}
                          className="font-mono text-[10px] bg-[#1f1f23] hover:bg-[#292a2d] text-[#ffb77d] px-2 py-0.5 rounded flex items-center gap-1 transition-colors cursor-pointer border border-[#292a2d]"
                        >
                          <Clock className="w-3 h-3" />
                          <span>{item.citation}</span>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Quick Interaction Input Field */}
            <form onSubmit={handleAskQuestion} className="flex flex-col gap-2 pt-1">
              <div className="relative">
                <input
                  type="text"
                  value={questionInput}
                  onChange={(e) => setQuestionInput(e.target.value)}
                  placeholder="Kaynak içi bir kavram veya iddia sorgulayın..."
                  className="w-full bg-[#0d0e11] px-3.5 py-2.5 pr-10 rounded-xl font-sans text-xs text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none focus:ring-1 focus:ring-[#ffb77d] border border-[#292a2d] transition-all"
                />
                <button
                  type="submit"
                  disabled={isAsking || !questionInput.trim()}
                  className="absolute right-1.5 top-1.5 bottom-1.5 px-2.5 bg-[#ffb77d] text-[#4d2600] rounded-lg font-mono text-xs flex items-center justify-center hover:bg-[#ffdcc3] transition-colors disabled:opacity-40 cursor-pointer"
                >
                  {isAsking ? (
                    <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  ) : (
                    <ArrowRight className="w-3.5 h-3.5" />
                  )}
                </button>
              </div>
            </form>
          </div>
        </aside>
      </div>}

      {/* Video preview modal */}
      {isVideoModalOpen && displayedSource && (
        <div
          className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4"
          onClick={() => setIsVideoModalOpen(false)}
        >
          <div
            className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl overflow-hidden max-w-3xl w-full shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="p-4 bg-[#0d0e11] border-b border-[#292a2d] flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-[#d97707]"></span>
                <span className="font-mono text-xs text-[#e3e2e6] font-semibold truncate">
                  {displayedSource.title}
                </span>
              </div>
              <button
                onClick={() => setIsVideoModalOpen(false)}
                className="text-[#a38c7c] hover:text-[#e3e2e6] text-xs font-mono px-2 py-1 bg-[#1f1f23] rounded cursor-pointer"
              >
                Kapat [ESC]
              </button>
            </div>

            <div className="relative aspect-video bg-black flex items-center justify-center">
              <img
                src={displayedSource.heroImage || FALLBACK_SOURCE_IMAGE}
                onError={useFallbackSourceImage}
                alt="Video preview"
                className="w-full h-full object-cover opacity-80"
              />
              <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 bg-black/40">
                <div className="w-16 h-16 rounded-full bg-[#ffb77d] text-[#4d2600] flex items-center justify-center shadow-2xl">
                  <Play className="w-8 h-8 fill-current ml-1" />
                </div>
                <div className="text-center px-4">
                  <p className="font-sans text-sm text-[#FAF7F2] font-semibold">
                    Simüle Edilmiş Araştırma Oynatıcısı
                  </p>
                  <p className="font-mono text-xs text-[#dbc2b0]/70 mt-1">
                    Transkript senkronizasyonu ve zaman damgalı notlar için Kaynak Detayı ekranına geçebilirsiniz.
                  </p>
                </div>
                <button
                  onClick={() => {
                    setIsVideoModalOpen(false);
                    onNavigate('kaynak-detayi', displayedSource.id);
                  }}
                  className="px-4 py-2 bg-[#d97707] text-white rounded-lg font-sans text-xs font-semibold hover:bg-[#b45309] transition-colors"
                >
                  Kaynak Detayında Aç
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
