import React, { useState, useEffect } from 'react';
import { Sidebar } from './components/Sidebar';
import { TopHeader } from './components/TopHeader';
import { QuickCaptureScreen } from './components/QuickCaptureScreen';
import { SourcesScreen } from './components/SourcesScreen';
import { SourceDetailScreen } from './components/SourceDetailScreen';
import { AiAssistantScreen } from './components/AiAssistantScreen';
import { CommandPaletteModal } from './components/CommandPaletteModal';
import { NotificationsModal } from './components/NotificationsModal';
import { INITIAL_SOURCES } from './data/sourcesData';
import { NavigationTab, SourceItem } from './types';
import { CheckCircle2 } from 'lucide-react';
import { createContentUseCase } from './app/dependencies';

export default function App() {
  const [sources, setSources] = useState<SourceItem[]>(INITIAL_SOURCES);
  const [currentSourceId, setCurrentSourceId] = useState<string>('karpathy-llm-101');
  const [aiSelectedSourceId, setAiSelectedSourceId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<NavigationTab>('hizli-yakala');
  const [selectedSeekSeconds, setSelectedSeekSeconds] = useState<number>(0);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState<boolean>(false);

  // Modals & UI states
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);

  // Toast feedback
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => {
      setToastMessage(null);
    }, 3000);
  };

  const currentSource =
    sources.find((s) => s.id === currentSourceId) || sources[0];

  const handleNavigate = (
    tab: NavigationTab,
    targetSourceId?: string,
    seekSeconds?: number
  ) => {
    if (targetSourceId) {
      setCurrentSourceId(targetSourceId);
      if (tab === 'ai-asistan') {
        setAiSelectedSourceId(targetSourceId);
      }
    } else if (tab === 'ai-asistan') {
      // Default to general synthesis across all sources if no target specified
      setAiSelectedSourceId(null);
    }
    if (typeof seekSeconds === 'number') {
      setSelectedSeekSeconds(seekSeconds);
    }
    setActiveTab(tab);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleAddNewSource = async (url: string, type: 'youtube' | 'web') => {
    const content = await createContentUseCase.execute(url, type);
    const isYt = type === 'youtube' || url.includes('youtube.com') || url.includes('youtu.be');
    const newSource: SourceItem = {
      id: content.id,
      type: isYt ? 'youtube' : 'web',
      title: content.title,
      originalUrl: url,
      duration: isYt ? 'Video' : 'Web makalesi',
      reliability: 100,
      synthesisNumber: `#${content.id.slice(0, 8).toUpperCase()}`,
      category: content.tags.join(' & ').toLocaleUpperCase('tr') || 'GENEL ARAŞTIRMA',
      heroImage: content.imageUrl ?? (isYt
        ? 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=1200&auto=format&fit=crop'
        : 'https://images.unsplash.com/photo-1516116211227-bbc13c7a52e6?q=80&w=1200&auto=format&fit=crop'),
      author: {
        name: 'Hafıza',
        role: 'Kaynak tabanlı analiz',
        avatarUrl:
          'https://images.unsplash.com/photo-1534528741775-53994a69daeb?q=80&w=200&auto=format&fit=crop',
      },
      executiveSummary: [content.summary],
      findings: content.keyPoints.map((point, index) => ({ id: `${content.id}-${index}`, phase: `${String(index + 1).padStart(2, '0')} / BULGU`, timestamp: '[Kaynak]', timeSeconds: 0, title: `Kritik bulgu ${index + 1}`, description: point, confidence: 'Kaynak metninden sentezlendi' })),
      qaPairs: [],
      transcript: [
        {
          time: '03:15',
          seconds: 195,
          speaker: 'Dr. Selin Demir',
          text: content.rawText,
          highlighted: true,
        },
      ],
      chunks: [
        {
          id: 'nchk-01',
          text: content.rawText,
          tokens: Math.ceil(content.rawText.length / 4),
          dimension: 1536,
          similarity: 0.97,
          category: 'Vektör Bellek',
        },
      ],
      telemetry: {
        chunksCount: 32,
        referencedNamesCount: 9,
        wordsCount: String(content.rawText.trim().split(/\s+/).length),
        vectorDimensions: 1536,
        matchPercentage: 100,
      },
      tags: content.tags,
      dateAdded: content.createdAt.toISOString().split('T')[0],
    };

    setSources((prev) => [newSource, ...prev]);
    setCurrentSourceId(content.id);
    showToast(`✓ Yeni kaynak indekslendi ve brifing hazırlandı! (${newSource.title})`);
    return newSource;
  };

  const handleSync = () => {
    setIsSyncing(true);
    setTimeout(() => {
      setIsSyncing(false);
      showToast('✓ Tüm vektör indeksleri ve transkript kanalları senkronize edildi.');
    }, 1200);
  };

  // Keyboard shortcut for command palette
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        setIsCommandPaletteOpen((prev) => !prev);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, []);

  return (
    <div className="min-h-screen bg-[#121316] text-[#e3e2e6] font-sans antialiased">
      {/* Toast Banner */}
      {toastMessage && (
        <div className="fixed top-16 right-6 z-50 bg-[#ffb77d] text-[#4d2600] px-4 py-2.5 rounded-xl font-sans text-xs font-bold shadow-2xl flex items-center gap-2 border border-[#ffdcc3] animate-bounce">
          <CheckCircle2 className="w-4 h-4 text-[#4d2600]" />
          <span>{toastMessage}</span>
        </div>
      )}

      {/* Main Sidebar */}
      <Sidebar
        activeTab={activeTab}
        onTabChange={(tab) => handleNavigate(tab)}
        sourcesCount={sources.length}
        isCollapsed={isSidebarCollapsed}
        onToggleCollapse={() => setIsSidebarCollapsed((prev) => !prev)}
      />

      {/* Workspace Wrapper shifted cleanly past the sidebar */}
      <div
        className={`min-h-screen flex flex-col transition-all duration-200 ${
          isSidebarCollapsed ? 'ml-16' : 'ml-64'
        }`}
      >
        {/* Top Header */}
        <TopHeader
          activeTab={activeTab}
          onOpenNotifications={() => setIsNotificationsOpen(true)}
          onSync={handleSync}
          isSyncing={isSyncing}
          isSidebarCollapsed={isSidebarCollapsed}
          onToggleSidebar={() => setIsSidebarCollapsed((prev) => !prev)}
        />

        {/* Primary Workspace View Area */}
        <main
          id="main-workspace-content"
          className="flex-1 pt-14 px-4 sm:px-6 md:px-8 py-6 w-full max-w-7xl mx-auto flex flex-col"
        >
          {activeTab === 'hizli-yakala' && (
            <QuickCaptureScreen
              currentSource={currentSource}
              onNavigate={handleNavigate}
              onAddNewSource={handleAddNewSource}
            />
          )}

          {activeTab === 'kaynaklarim' && (
            <SourcesScreen
              sources={sources}
              onSelectSource={(id) => setCurrentSourceId(id)}
              onNavigate={handleNavigate}
            />
          )}

          {activeTab === 'kaynak-detayi' && (
            <SourceDetailScreen
              source={currentSource}
              initialSeekSeconds={selectedSeekSeconds}
              onNavigate={handleNavigate}
            />
          )}

          {activeTab === 'ai-asistan' && (
            <AiAssistantScreen
              sources={sources}
              selectedSourceId={aiSelectedSourceId || undefined}
              onClearSelectedSource={() => setAiSelectedSourceId(null)}
              onNavigate={handleNavigate}
            />
          )}
        </main>
      </div>

      {/* Modals */}
      <CommandPaletteModal
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        sources={sources}
        onSelectSource={(id) => setCurrentSourceId(id)}
        onNavigate={handleNavigate}
      />

      <NotificationsModal
        isOpen={isNotificationsOpen}
        onClose={() => setIsNotificationsOpen(false)}
      />
    </div>
  );
}
