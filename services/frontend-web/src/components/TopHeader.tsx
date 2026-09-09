import React from 'react';
import { RefreshCw, Bell, PanelLeftClose, PanelLeft } from 'lucide-react';
import { NavigationTab } from '../types';

interface TopHeaderProps {
  activeTab: NavigationTab;
  onOpenNotifications: () => void;
  onSync: () => void;
  isSyncing: boolean;
  isSidebarCollapsed: boolean;
  onToggleSidebar: () => void;
}

export const TopHeader: React.FC<TopHeaderProps> = ({
  activeTab,
  onOpenNotifications,
  onSync,
  isSyncing,
  isSidebarCollapsed,
  onToggleSidebar,
}) => {
  const getBreadcrumb = () => {
    switch (activeTab) {
      case 'hizli-yakala':
        return { section: 'ARAŞTIRMA', sub: 'ANALİTİK ÇALIŞMA ALANI' };
      case 'kaynaklarim':
        return { section: 'KAYNAKLAR', sub: 'BİLGİ HAVUZU & ARŞİV' };
      case 'kaynak-detayi':
        return { section: 'DETAY', sub: 'TRANSKRİPT & VEKTÖR DÖKÜMÜ' };
      case 'ai-asistan':
        return { section: 'ASİSTAN', sub: 'ÇAPRAZ SENTEZ & ANALİZ' };
      default:
        return { section: 'ARAŞTIRMA', sub: 'ANALİTİK ÇALIŞMA ALANI' };
    }
  };

  const breadcrumb = getBreadcrumb();

  return (
    <header
      id="top-header"
      className={`fixed top-0 right-0 h-14 bg-[#0d0e11]/95 backdrop-blur-md z-40 px-4 md:px-6 flex items-center justify-between border-b border-[#1f1f23] transition-all duration-200 ${
        isSidebarCollapsed ? 'left-16' : 'left-64'
      }`}
    >
      {/* Breadcrumbs & Collapse Toggle */}
      <div className="flex items-center gap-2 md:gap-3">
        <button
          onClick={onToggleSidebar}
          title={isSidebarCollapsed ? 'Kenar Çubuğunu Genişlet' : 'Kenar Çubuğunu Daralt'}
          className="p-1.5 text-[#a38c7c] hover:text-[#e3e2e6] hover:bg-[#1f1f23] rounded-lg transition-colors cursor-pointer"
        >
          {isSidebarCollapsed ? (
            <PanelLeft className="w-4 h-4 text-[#ffb77d]" />
          ) : (
            <PanelLeftClose className="w-4 h-4" />
          )}
        </button>

        <div className="flex items-center gap-2">
          <span className="font-mono text-[11px] md:text-xs font-medium text-[#a38c7c] tracking-wider">
            {breadcrumb.section}
          </span>
          <span className="text-[#a38c7c]/60 text-xs">/</span>
          <span className="font-mono text-[11px] md:text-xs font-semibold text-[#e3e2e6] tracking-wider">
            {breadcrumb.sub}
          </span>
        </div>
      </div>

      {/* Right Actions */}
      <div className="flex items-center gap-2.5 md:gap-3">
        <button
          id="header-sync-btn"
          title="İndeksleri Senkronize Et"
          onClick={onSync}
          className="p-1.5 text-[#dbc2b0] hover:text-[#e3e2e6] hover:bg-[#1f1f23] rounded-md transition-colors cursor-pointer"
        >
          <RefreshCw className={`w-4 h-4 ${isSyncing ? 'animate-spin text-[#ffb77d]' : ''}`} />
        </button>

        <button
          id="header-notifications-btn"
          title="Bildirimler"
          onClick={onOpenNotifications}
          className="p-1.5 text-[#dbc2b0] hover:text-[#e3e2e6] hover:bg-[#1f1f23] rounded-md transition-colors relative cursor-pointer"
        >
          <Bell className="w-4 h-4" />
          <span className="absolute top-1 right-1 w-2 h-2 bg-[#d97707] rounded-full ring-2 ring-[#0d0e11]"></span>
        </button>
      </div>
    </header>
  );
};
