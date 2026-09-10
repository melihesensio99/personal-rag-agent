import React, { useState, useEffect } from 'react';
import {
  Brain,
  Send,
  Copy,
  Check,
  Layers,
  X,
} from 'lucide-react';
import { SourceItem, NavigationTab } from '../types';
import { askSemanticQuestion } from '../app/dependencies';

interface AiAssistantScreenProps {
  sources: SourceItem[];
  selectedSourceId?: string;
  onClearSelectedSource?: () => void;
  onNavigate: (tab: NavigationTab, targetSourceId?: string) => void;
}

interface Message {
  id: string;
  sender: 'user' | 'assistant';
  text: string;
  timestamp: string;
  citations?: Array<{
    sourceId: string;
    sourceTitle: string;
    timeOrSection: string;
    snippet: string;
    similarity: number;
  }>;
}

export const AiAssistantScreen: React.FC<AiAssistantScreenProps> = ({
  sources,
  selectedSourceId,
  onClearSelectedSource,
  onNavigate,
}) => {
  const [focusedSourceId, setFocusedSourceId] = useState<string | null>(selectedSourceId || null);
  const [inputQuery, setInputQuery] = useState('');
  const [isSynthesizing, setIsSynthesizing] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  useEffect(() => {
    setFocusedSourceId(selectedSourceId || null);
  }, [selectedSourceId]);

  const focusedSource = sources.find((s) => s.id === focusedSourceId) || null;

  const [messages, setMessages] = useState<Message[]>([]);

  const handleSend = async (textToSend?: string) => {
    const query = textToSend || inputQuery;
    if (!query.trim() || isSynthesizing) return;

    const userMsg: Message = {
      id: `u-${Date.now()}`,
      sender: 'user',
      text: query,
      timestamp: new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
    };

    setMessages((prev) => [...prev, userMsg]);
    setInputQuery('');
    setIsSynthesizing(true);

    try {
      const response = await askSemanticQuestion(query, focusedSource?.id);
      const citations: Message['citations'] = response.sources.map((source) => ({
        sourceId: source.contentId,
        sourceTitle: source.contentTitle,
        timeOrSection: `Chunk ${source.chunkIndex + 1}`,
        snippet: source.chunkText.length > 140 ? `${source.chunkText.slice(0, 140)}...` : source.chunkText,
        similarity: source.similarity,
      }));

      setMessages((prev) => [...prev, {
        id: `a-${Date.now()}`,
        sender: 'assistant',
        text: response.answer,
        timestamp: new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
        citations,
      }]);
    } catch (error) {
      setMessages((prev) => [...prev, {
        id: `a-${Date.now()}`,
        sender: 'assistant',
        text: error instanceof Error ? error.message : 'Soru cevaplanamadı. Lütfen tekrar deneyin.',
        timestamp: new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
      }]);
    } finally {
      setIsSynthesizing(false);
    }
  };

  const handleCopy = (id: string, text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  return (
    <div id="ai-assistant-screen" className="flex flex-col gap-6 w-full max-w-5xl mx-auto">
      {/* Studio Header */}
      <div className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl p-6 shadow-md flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-3.5">
          <div className="w-12 h-12 rounded-xl bg-[#ffb77d]/15 border border-[#ffb77d]/30 text-[#ffb77d] flex items-center justify-center shrink-0">
            <Brain className="w-6 h-6" />
          </div>
          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <h1 className="font-serif text-2xl text-[#e3e2e6] font-semibold">
                Çapraz Sentez & AI Asistan
              </h1>
              <span className="font-mono text-[10px] bg-[#d97707]/20 text-[#ffb77d] border border-[#d97707]/30 px-2 py-0.5 rounded uppercase">
                Grounded RAG
              </span>
            </div>
            <p className="font-sans text-xs text-[#dbc2b0]/80 mt-1 max-w-2xl leading-relaxed">
              Havuzdaki tüm video, makale ve akademik transkriptler üzerinde eş zamanlı anlamsal tarama gerçekleştirin; tezleri karşılaştırıp doğrudan kaynak alıntılarıyla temellendirilmiş derinlemesine sentez üretin.
            </p>
          </div>
        </div>

        {/* Scope Indicator (only shown in global all sources mode) */}
        {!focusedSource && (
          <div className="flex items-center gap-2.5 bg-[#0d0e11] border border-[#292a2d] px-3.5 py-2 rounded-xl shrink-0">
            <Layers className="w-3.5 h-3.5 text-[#ffb77d]" />
            <span className="font-mono text-[11px] text-[#a38c7c]">Kapsam:</span>
            <span className="font-mono text-[11px] text-[#e3e2e6] font-medium flex items-center gap-1.5">
              <span className="w-1.5 h-1.5 rounded-full bg-[#10b981]"></span>
              Tüm Kaynaklar ({sources.length})
            </span>
          </div>
        )}
      </div>

      {/* Focused Source Context Banner */}
      {focusedSource && (
        <div
          id="focused-source-card-banner"
          className="bg-[#1b1b1f] border border-[#ffb77d]/35 rounded-2xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-md"
        >
          <div className="flex items-center gap-3.5 min-w-0 flex-1">
            {focusedSource.author.avatarUrl ? (
              <img
                src={focusedSource.author.avatarUrl}
                alt={focusedSource.author.name}
                className="w-10 h-10 rounded-full object-cover shrink-0 ring-2 ring-[#ffb77d]/40"
              />
            ) : (
              <div
                className="w-10 h-10 rounded-full shrink-0 ring-2 ring-[#ffb77d]/40 bg-[#ffb77d]/15 border border-[#ffb77d]/40 flex items-center justify-center text-[#ffb77d]"
                aria-label={`${focusedSource.author.name} avatarı`}
              >
                <Brain className="w-5 h-5" />
              </div>
            )}
            <div className="flex flex-col min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-sans text-sm font-bold text-[#e3e2e6] min-w-0 truncate">
                  {focusedSource.title}
                </span>
                <span className="font-mono text-[10px] bg-[#ffb77d]/15 text-[#ffb77d] px-2 py-0.5 rounded border border-[#ffb77d]/30 max-w-full truncate">
                  {focusedSource.category}
                </span>
              </div>
              <span className="font-sans text-xs text-[#dbc2b0]/80 mt-0.5">
                <strong className="text-[#ffb77d] font-medium">{focusedSource.author.name}</strong> ({focusedSource.author.role}) — Doğrudan bu kaynağın transkripti, özetleri ve analitik bulguları üzerinde soru soruyorsunuz.
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
            <button
              id="btn-remove-source-filter-banner"
              onClick={() => {
                setFocusedSourceId(null);
                if (onClearSelectedSource) onClearSelectedSource();
              }}
              className="flex items-center gap-1.5 px-3.5 py-2 bg-[#0d0e11] hover:bg-[#292a2d] text-[#ffb77d] hover:text-[#ffdcc3] border border-[#ffb77d]/30 rounded-xl font-sans text-xs font-semibold transition-colors cursor-pointer"
            >
              <X className="w-3.5 h-3.5" />
              <span>Filtreyi Kaldır</span>
            </button>
          </div>
        </div>
      )}

      {/* Chat Thread */}
      <div className="flex flex-col gap-4 bg-[#121316] rounded-2xl">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`flex flex-col gap-2 p-5 rounded-2xl border ${
              msg.sender === 'user'
                ? 'bg-[#1b1b1f] border-[#292a2d] ml-8'
                : 'bg-[#1f1f23] border-[#292a2d] shadow-lg mr-8'
            }`}
          >
            {/* Header info */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span
                  className={`w-6 h-6 rounded-full flex items-center justify-center font-mono text-[10px] font-bold ${
                    msg.sender === 'user'
                      ? 'bg-[#292a2d] text-[#e3e2e6]'
                      : 'bg-[#ffb77d] text-[#4d2600]'
                  }`}
                >
                  {msg.sender === 'user' ? 'S' : 'AI'}
                </span>
                <span className="font-sans text-xs font-semibold text-[#e3e2e6]">
                  {msg.sender === 'user' ? 'Araştırmacı' : 'Hafıza Sentez Motoru'}
                </span>
                <span className="font-mono text-[10px] text-[#a38c7c]">{msg.timestamp}</span>
              </div>

              {msg.sender === 'assistant' && (
                <button
                  onClick={() => handleCopy(msg.id, msg.text)}
                  className="p-1 text-[#a38c7c] hover:text-[#e3e2e6] rounded transition-colors cursor-pointer"
                >
                  {copiedId === msg.id ? (
                    <Check className="w-3.5 h-3.5 text-emerald-400" />
                  ) : (
                    <Copy className="w-3.5 h-3.5" />
                  )}
                </button>
              )}
            </div>

            {/* Message Body */}
            <div className="text-sm font-sans text-[#e3e2e6] whitespace-pre-wrap leading-relaxed pt-1">
              {msg.text}
            </div>

            {/* Citations block */}
            {msg.citations && msg.citations.length > 0 && (
              <div className="mt-3 pt-3 border-t border-[#292a2d] flex flex-col gap-2">
                <span className="font-mono text-[10px] text-[#ffb77d] font-semibold uppercase tracking-wider">
                  Doğrulanmış Kaynak Kanıtları:
                </span>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                  {msg.citations.map((c, idx) => (
                    <div
                      key={idx}
                      onClick={() => onNavigate('kaynak-detayi', c.sourceId)}
                      className="bg-[#0d0e11] hover:bg-[#18191d] p-2.5 rounded-xl border border-[#292a2d] flex flex-col gap-1 cursor-pointer transition-colors"
                    >
                      <div className="flex items-center justify-between font-mono text-[10px]">
                        <span className="text-[#ffb77d] font-semibold truncate">{c.sourceTitle}</span>
                        <span className="text-[#a38c7c] bg-[#1f1f23] px-1.5 py-0.5 rounded">
                          {c.timeOrSection}
                        </span>
                      </div>
                      <span className="font-mono text-[10px] text-emerald-300/80">
                        Benzerlik: %{Math.round(Math.max(0, Math.min(1, c.similarity)) * 100)}
                      </span>
                      <p className="font-sans text-[11px] text-[#dbc2b0]/70 italic line-clamp-1">
                        "{c.snippet}"
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        ))}

        {isSynthesizing && (
          <div className="p-4 bg-[#1f1f23] border border-[#292a2d] rounded-2xl flex items-center gap-3 mr-8 text-xs font-mono text-[#ffb77d]">
            <Brain className="w-4 h-4 animate-spin" />
            <span>
              {focusedSource
                ? `"${focusedSource.author.name}" transkripti taranıyor ve analiz oluşturuluyor...`
                : 'Vektör uzayı taranıyor ve kanıta dayalı sentez oluşturuluyor...'}
            </span>
          </div>
        )}
      </div>

      {/* Input Form */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          handleSend();
        }}
        className="sticky bottom-4 bg-[#1b1b1f] p-2.5 rounded-2xl border border-[#292a2d] shadow-2xl flex items-center gap-3"
      >
        <input
          type="text"
          value={inputQuery}
          onChange={(e) => setInputQuery(e.target.value)}
          placeholder={
            focusedSource
              ? `"${focusedSource.author.name}" kaynağı hakkında soru sorun...`
              : 'Tüm kaynaklar üzerinde derin bir soru sorun veya kavramları karşılaştırın...'
          }
          className="bg-transparent font-sans text-xs md:text-sm text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none px-3 flex-1"
        />
        <button
          type="submit"
          disabled={!inputQuery.trim() || isSynthesizing}
          className="px-4 py-2.5 bg-[#ffb77d] text-[#4d2600] rounded-xl font-sans text-xs font-bold hover:bg-[#ffdcc3] transition-all flex items-center gap-2 cursor-pointer disabled:opacity-40"
        >
          <span>Sentezle</span>
          <Send className="w-3.5 h-3.5" />
        </button>
      </form>
    </div>
  );
};
