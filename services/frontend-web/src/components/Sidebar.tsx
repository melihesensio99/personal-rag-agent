import React from 'react';
import {
  Zap,
  BookOpen,
  FileSearch,
  Brain,
  Sparkles,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { NavigationTab } from '../types';

interface SidebarProps {
  activeTab: NavigationTab;
  onTabChange: (tab: NavigationTab) => void;
  sourcesCount: number;
  isCollapsed?: boolean;
  onToggleCollapse?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({
  activeTab,
  onTabChange,
  sourcesCount,
  isCollapsed = false,
  onToggleCollapse,
}) => {
  const navItems: Array<{
    id: NavigationTab;
    label: string;
    icon: React.ElementType;
    badge?: string;
  }> = [
    { id: 'hizli-yakala', label: 'Hızlı Yakala', icon: Zap },
    { id: 'kaynaklarim', label: 'Kaynaklarım', icon: BookOpen, badge: String(sourcesCount) },
    { id: 'kaynak-detayi', label: 'Kaynak Detayı', icon: FileSearch },
    { id: 'ai-asistan', label: 'AI Asistan', icon: Brain, badge: 'Canlı' },
  ];

  return (
    <aside
      id="main-sidebar"
      className={`fixed left-0 top-0 h-full bg-[#0d0e11] z-50 flex flex-col justify-between border-r border-[#1f1f23] transition-all duration-200 ${
        isCollapsed ? 'w-16 p-2.5 items-center' : 'w-64 p-4'
      }`}
    >
      <div className="flex flex-col gap-6 w-full">
        {/* Brand Header */}
        <div className="flex items-center justify-between">
          <div
            id="sidebar-logo-container"
            onClick={() => onTabChange('hizli-yakala')}
            className={`flex items-center gap-3 cursor-pointer select-none group ${
              isCollapsed ? 'justify-center w-full' : 'px-1.5'
            }`}
          >
            <img
              alt="Hafıza Araştırma Alanı Logosu"
              className="h-8 w-8 object-contain transition-transform group-hover:scale-105 shrink-0"
              src="https://lh3.googleusercontent.com/aida/AEtjO1VFIOoSZ3Kwsn8t-MgHwi21h0e2KJU3bd0IasVrYkF9tLSALCjR7hiuGD-wHtEds3yJUxQ1YO-NPY0r6b5ZJDrX0R8eu594sOIsrRkgHkBW2lcLWnOX_fWjUQXXN7UgskpwJUOvpQuc02on72-5a8s02yL4SuTPfTTNBl1dN6MCUS2Nbr43q7RA5HKAvuXc22gZMY3Y_y0AR2m3GyCu0ov1FTgWigsTLiwV1E-5sjBWjnNVW7apq31ENJA"
            />
            {!isCollapsed && (
              <div className="flex flex-col">
                <span className="font-sans font-bold text-base tracking-tight text-[#e3e2e6] flex items-center gap-1.5">
                  Hafıza
                  <span className="inline-flex items-center px-1.5 py-0.5 rounded text-[9px] font-mono font-medium bg-[#d97707]/20 text-[#ffb77d] border border-[#d97707]/30">
                    PRO
                  </span>
                </span>
                <span className="font-mono text-[10px] uppercase text-[#ffb77d] tracking-widest font-semibold">
                  Araştırma Alanı
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Navigation items */}
        <nav id="sidebar-nav" className="flex flex-col gap-1.5 w-full">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeTab === item.id;
            return (
              <button
                key={item.id}
                id={`nav-${item.id}`}
                title={isCollapsed ? item.label : undefined}
                onClick={() => onTabChange(item.id)}
                className={`flex items-center rounded-xl text-sm transition-all text-left cursor-pointer ${
                  isCollapsed
                    ? 'justify-center p-2.5'
                    : 'justify-between px-3.5 py-2.5'
                } ${
                  isActive
                    ? 'bg-[#292a2d] text-[#ffb77d] font-semibold shadow-inner border-l-2 border-[#d97707]'
                    : 'text-[#dbc2b0] hover:bg-[#1f1f23] hover:text-[#e3e2e6]'
                }`}
              >
                <div className={`flex items-center gap-3 min-w-0 ${isCollapsed ? 'justify-center' : ''}`}>
                  <Icon
                    className={`w-4 h-4 shrink-0 transition-colors ${
                      isActive ? 'text-[#ffb77d]' : 'text-[#a38c7c]'
                    }`}
                  />
                  {!isCollapsed && (
                    <span className="truncate font-sans font-medium text-[13px]">{item.label}</span>
                  )}
                </div>
                {!isCollapsed && item.badge && (
                  <span
                    className={`text-[10px] font-mono px-1.5 py-0.5 rounded ${
                      isActive
                        ? 'bg-[#ffb77d]/20 text-[#ffb77d]'
                        : 'bg-[#1b1b1f] text-[#a38c7c]'
                    }`}
                  >
                    {item.badge}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Footer / User Profile Card & Toggle */}
      <div className="flex flex-col gap-2 w-full">
        {onToggleCollapse && (
          <button
            onClick={onToggleCollapse}
            title={isCollapsed ? 'Kenar Çubuğunu Genişlet' : 'Kenar Çubuğunu Daralt'}
            className="flex items-center justify-center py-2 text-[#a38c7c] hover:text-[#e3e2e6] hover:bg-[#1f1f23] rounded-lg transition-colors cursor-pointer w-full text-xs font-mono"
          >
            {isCollapsed ? (
              <ChevronRight className="w-4 h-4" />
            ) : (
              <div className="flex items-center gap-2">
                <ChevronLeft className="w-4 h-4" />
                <span>Çubuğu Daralt</span>
              </div>
            )}
          </button>
        )}

        <div
          className={`flex items-center gap-2.5 bg-[#1b1b1f] rounded-xl border border-[#292a2d] ${
            isCollapsed ? 'p-2 justify-center' : 'p-3'
          }`}
        >
          <img
            alt="Profile"
            className="w-8 h-8 rounded-full object-cover ring-1 ring-[#ffb77d]/30 shrink-0"
            src="https://lh3.googleusercontent.com/aida/AEtjO1WiMGPAmPr6gKULYHP6qrGrurEv91NwxkyAm0D52D6nCJLCXODKAmZnLsS5ZUwNm3TLp3agvVKsPPATfly7iPsPRqDasYL-YDrqA2aiy2Qc-9AiH3XgqrKBc_6O3fefQtgs0-Us0lloWpU3qUYuKhfrFYHPZDDbKpQOmvX5aBJY_-ZnMkzaKIl81JYqqv2O9KIBEvvNQjVK4sCCahFTe1OaJi7lJCXQocSFP6F0nNDKzZ6-P8PrB9SFbic"
          />

          {!isCollapsed && (
            <div className="flex flex-col min-w-0 flex-1">
              <span className="font-sans text-xs font-semibold text-[#e3e2e6] truncate">
                Emre Kaya
              </span>
              <span className="font-mono text-[10px] text-[#dbc2b0]/70 truncate">
                Baş Araştırmacı
              </span>
            </div>
          )}
        </div>
      </div>
    </aside>
  );
};
