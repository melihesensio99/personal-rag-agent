import React, { useState, useEffect } from 'react';
import { Search, Zap, BookOpen, Brain, X, ArrowRight } from 'lucide-react';
import { SourceItem, NavigationTab } from '../types';

interface CommandPaletteModalProps {
  isOpen: boolean;
  onClose: () => void;
  sources: SourceItem[];
  onSelectSource: (sourceId: string) => void;
  onNavigate: (tab: NavigationTab) => void;
}

export const CommandPaletteModal: React.FC<CommandPaletteModalProps> = ({
  isOpen,
  onClose,
  sources,
  onSelectSource,
  onNavigate,
}) => {
  const [query, setQuery] = useState('');

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        if (isOpen) onClose();
      }
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const filteredSources = sources.filter(
    (s) =>
      s.title.toLowerCase().includes(query.toLowerCase()) ||
      s.author.name.toLowerCase().includes(query.toLowerCase()) ||
      s.category.toLowerCase().includes(query.toLowerCase())
  );

  const navigationCommands: Array<{
    id: NavigationTab;
    title: string;
    icon: React.ElementType;
    desc: string;
  }> = [
    { id: 'hizli-yakala', title: 'Hızlı Yakala (Araştırma Masası)', icon: Zap, desc: 'URL analizi ve yönetici sentezi' },
    { id: 'kaynaklarim', title: 'Kaynaklarım Arşivi', icon: BookOpen, desc: 'Tüm indekslenmiş dokümanlar' },
    { id: 'ai-asistan', title: 'AI Asistan (Çapraz Sentez)', icon: Brain, desc: 'Kaynaklar arası soru-cevap' },
  ];

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-xs z-50 flex items-start justify-center pt-24 p-4"
      onClick={onClose}
    >
      <div
        className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl w-full max-w-xl shadow-2xl overflow-hidden flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Search Input */}
        <div className="flex items-center gap-3 px-4 py-3.5 border-b border-[#292a2d] bg-[#0d0e11]">
          <Search className="w-5 h-5 text-[#ffb77d]" />
          <input
            autoFocus
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Kaynak, komut veya ekran ara..."
            className="bg-transparent font-sans text-sm text-[#e3e2e6] placeholder:text-[#a38c7c]/50 focus:outline-none w-full"
          />
          <button
            onClick={onClose}
            className="text-[#a38c7c] hover:text-[#e3e2e6] p-1 rounded transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Results List */}
        <div className="max-h-96 overflow-y-auto p-2 flex flex-col gap-1">
          {/* Section: Screens */}
          <span className="font-mono text-[10px] uppercase text-[#a38c7c] px-3 py-1.5 tracking-wider">
            Ekranlar & Modüller
          </span>
          {navigationCommands.map((cmd) => {
            const Icon = cmd.icon;
            return (
              <button
                key={cmd.id}
                onClick={() => {
                  onNavigate(cmd.id);
                  onClose();
                }}
                className="flex items-center justify-between px-3 py-2.5 rounded-xl hover:bg-[#292a2d] text-left transition-colors cursor-pointer group"
              >
                <div className="flex items-center gap-3">
                  <div className="w-7 h-7 rounded-lg bg-[#1f1f23] text-[#ffb77d] flex items-center justify-center group-hover:bg-[#ffb77d] group-hover:text-[#4d2600] transition-colors">
                    <Icon className="w-4 h-4" />
                  </div>
                  <div className="flex flex-col">
                    <span className="font-sans text-xs font-semibold text-[#e3e2e6]">
                      {cmd.title}
                    </span>
                    <span className="font-sans text-[11px] text-[#a38c7c]">{cmd.desc}</span>
                  </div>
                </div>
                <ArrowRight className="w-3.5 h-3.5 text-[#a38c7c] opacity-0 group-hover:opacity-100 transition-opacity" />
              </button>
            );
          })}

          {/* Section: Sources */}
          {filteredSources.length > 0 && (
            <>
              <span className="font-mono text-[10px] uppercase text-[#a38c7c] px-3 py-1.5 mt-2 tracking-wider">
                İndekslenmiş Kaynaklar
              </span>
              {filteredSources.map((source) => (
                <button
                  key={source.id}
                  onClick={() => {
                    onSelectSource(source.id);
                    onNavigate('hizli-yakala');
                    onClose();
                  }}
                  className="flex items-center justify-between px-3 py-2 rounded-xl hover:bg-[#292a2d] text-left transition-colors cursor-pointer group"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <img
                      src={source.author.avatarUrl}
                      alt={source.author.name}
                      className="w-6 h-6 rounded-full object-cover shrink-0 ring-1 ring-[#ffb77d]/30"
                    />
                    <div className="flex flex-col min-w-0">
                      <span className="font-sans text-xs text-[#e3e2e6] font-medium truncate">
                        {source.title}
                      </span>
                      <span className="font-mono text-[10px] text-[#a38c7c]">
                        {source.author.name} • {source.duration}
                      </span>
                    </div>
                  </div>
                  <span className="font-mono text-[10px] bg-[#1f1f23] text-[#ffb77d] px-2 py-0.5 rounded shrink-0">
                    Brifingi Aç
                  </span>
                </button>
              ))}
            </>
          )}
        </div>

        {/* Footer */}
        <div className="px-4 py-2 border-t border-[#292a2d] bg-[#0d0e11] flex items-center justify-between font-mono text-[10px] text-[#a38c7c]">
          <span>Gezinmek için komutlara tıklayın</span>
          <span>[ESC] Kapat</span>
        </div>
      </div>
    </div>
  );
};
