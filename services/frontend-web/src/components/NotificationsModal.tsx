import React from 'react';
import { Bell, X } from 'lucide-react';

interface NotificationsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const NotificationsModal: React.FC<NotificationsModalProps> = ({ isOpen, onClose }) => {
  if (!isOpen) return null;

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
          <div className="bg-[#1f1f23] p-4 rounded-xl border border-[#292a2d] text-center">
            <p className="font-sans text-xs text-[#dbc2b0]/70 leading-normal">Henüz bildirim yok.</p>
          </div>
        </div>

        <div className="p-3 border-t border-[#292a2d] bg-[#0d0e11] text-center">
          <span className="font-sans text-xs text-[#a38c7c]">Gerçek bildirimler burada görünecek.</span>
        </div>
      </div>
    </div>
  );
};
