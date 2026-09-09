import React, { useState, useEffect } from 'react';
import { Sidebar } from './components/Sidebar';
import { TopHeader } from './components/TopHeader';
import { QuickCaptureScreen } from './components/QuickCaptureScreen';
import { SourcesScreen } from './components/SourcesScreen';
import { SourceDetailScreen } from './components/SourceDetailScreen';
import { AiAssistantScreen } from './components/AiAssistantScreen';
import { CommandPaletteModal } from './components/CommandPaletteModal';
import { NotificationsModal } from './components/NotificationsModal';
import { NavigationTab, SourceItem } from './types';
import { CheckCircle2 } from 'lucide-react';
import { contentRepository, createContentUseCase, getContentDetailUseCase, listContentsUseCase } from './app/dependencies';
import { mapContentToSourceItem } from './features/sources/presentation/sourceItemMapper';

export default function App() {
  const [sources, setSources] = useState<SourceItem[]>([]);
  const [currentSourceId, setCurrentSourceId] = useState<string>('');
  const [aiSelectedSourceId, setAiSelectedSourceId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<NavigationTab>('hizli-yakala');
  const [selectedSeekSeconds, setSelectedSeekSeconds] = useState<number>(0);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState<boolean>(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

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
    const newSource = mapContentToSourceItem(content);

    setSources((prev) => [newSource, ...prev]);
    setCurrentSourceId(content.id);
    showToast(`✓ Yeni kaynak indekslendi ve brifing hazırlandı! (${newSource.title})`);
    return newSource;
  };

  const handleDeleteSource = async (sourceId: string) => {
    await contentRepository.delete(sourceId);
    setSources((prev) => prev.filter((source) => source.id !== sourceId));
    if (currentSourceId === sourceId) setCurrentSourceId('');
    showToast('Kaynak arşivden silindi.');
  };

  useEffect(() => {
    let active = true;
    void listContentsUseCase.execute()
      .then((contents) => {
        if (!active) return;
        const loadedSources = contents.map((content) => mapContentToSourceItem(content));
        setSources(loadedSources);
        setCurrentSourceId(loadedSources[0]?.id ?? '');
      })
      .catch((reason) => {
        if (active) showToast(reason instanceof Error ? reason.message : 'Kaynaklar yüklenemedi.');
      });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (activeTab !== 'kaynak-detayi' || !currentSourceId) return;

    let active = true;
    setDetailLoading(true);
    setDetailError(null);

    void getContentDetailUseCase.execute(currentSourceId)
      .then(({ content, chunks }) => {
        if (!active) return;
        const detailedSource = mapContentToSourceItem(content, chunks);
        setSources((current) => current.map((source) =>
          source.id === detailedSource.id ? detailedSource : source
        ));
      })
      .catch((reason) => {
        if (!active) return;
        setDetailError(reason instanceof Error ? reason.message : 'Kaynak detayı yüklenemedi.');
      })
      .finally(() => {
        if (active) setDetailLoading(false);
      });

    return () => { active = false; };
  }, [activeTab, currentSourceId]);

  const handleSync = async () => {
    setIsSyncing(true);
    try {
      const contents = await listContentsUseCase.execute();
      const loadedSources = contents.map((content) => mapContentToSourceItem(content));
      setSources(loadedSources);
      if (currentSourceId && !loadedSources.some((source) => source.id === currentSourceId)) {
        setCurrentSourceId(loadedSources[0]?.id ?? '');
      }
      showToast(`✓ Kaynak listesi güncellendi (${loadedSources.length} kaynak).`);
    } catch (reason) {
      showToast(reason instanceof Error ? reason.message : 'Kaynaklar güncellenemedi.');
    } finally {
      setIsSyncing(false);
    }
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
              onDeleteSource={handleDeleteSource}
            />
          )}

          {activeTab === 'kaynak-detayi' && (
            currentSource ? <SourceDetailScreen source={currentSource} isLoading={detailLoading} error={detailError} initialSeekSeconds={selectedSeekSeconds} onNavigate={handleNavigate}/> :
              <div className="m-auto rounded-2xl border border-[#292a2d] bg-[#1b1b1f] p-8 text-center text-[#dbc2b0]">Henüz incelenecek bir kaynak yok.</div>
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
