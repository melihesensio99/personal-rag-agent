import React from 'react';
import { Bell, CheckCircle2, Clock, X, Sparkles, ShieldCheck } from 'lucide-react';

interface NotificationsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const NotificationsModal: React.FC<NotificationsModalProps> = ({ isOpen, onClose }) => {
  if (!isOpen) return null;

  const notifications = [
    {
      id: 'n-1',
      title: 'Andrej Karpathy LLM 101 İndekslendi',
      detail: '48 anlamsal chunk ve 4 temel hipotez vektör hafızasına başarıyla kaydedildi.',
      time: '12 dk önce',
      type: 'success',
    },
    {
      id: 'n-2',
      title: 'Hafıza İndeksi Senkronizasyonu Tamamlandı',
      detail: 'Toplam 198 chunk ve 14 teorik referans bağlantılandırıldı.',
      time: '1 saat önce',
      type: 'sync',
    },
    {
      id: 'n-3',
      title: 'Güvenilirlik Doğrulama Raporu',
      detail: 'Halüsinasyon filtresi %98.4 doğrulukla teyit edildi.',
      time: '3 saat önce',
      type: 'verify',
    },
  ];

  return (
    <div
      className="fixed inset-0 bg-black/80 backdrop-blur-xs z-50 flex items-start justify-end p-4 md:p-6"
      onClick={onClose}
    >
      <div
        className="bg-[#1b1b1f] border border-[#292a2d] rounded-2xl w-full max-w-sm shadow-2xl overflow-hidden mt-12 flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-[#292a2d] bg-[#0d0e11]">
          <div className="flex items-center gap-2">
            <Bell className="w-4 h-4 text-[#ffb77d]" />
            <span className="font-serif text-base text-[#e3e2e6] font-semibold">
              Kokpit Bildirimleri
            </span>
          </div>
          <button
            onClick={onClose}
            className="text-[#a38c7c] hover:text-[#e3e2e6] p-1 rounded transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="p-3 flex flex-col gap-2.5 max-h-96 overflow-y-auto">
          {notifications.map((n) => (
            <div
              key={n.id}
              className="bg-[#1f1f23] p-3 rounded-xl border border-[#292a2d] flex flex-col gap-1 hover:border-[#ffb77d]/30 transition-colors"
            >
              <div className="flex items-center justify-between">
                <span className="font-sans text-xs font-bold text-[#e3e2e6]">{n.title}</span>
                <span className="font-mono text-[9px] text-[#a38c7c]">{n.time}</span>
              </div>
              <p className="font-sans text-xs text-[#dbc2b0]/70 leading-normal">{n.detail}</p>
            </div>
          ))}
        </div>

        <div className="p-3 border-t border-[#292a2d] bg-[#0d0e11] text-center">
          <button
            onClick={onClose}
            className="font-sans text-xs text-[#ffb77d] hover:underline cursor-pointer"
          >
            Tümünü Okundu Olarak İşaretle
          </button>
        </div>
      </div>
    </div>
  );
};
