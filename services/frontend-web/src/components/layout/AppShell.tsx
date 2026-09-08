import type { PropsWithChildren } from "react";
import { BookOpen, BrainCircuit, Settings, Sparkles } from "lucide-react";
import { NavLink } from "react-router-dom";

const navigation = [
  { to: "/quick-summary", label: "Hızlı Özet", icon: Sparkles },
  { to: "/library", label: "Kaynaklarım", icon: BookOpen },
  { to: "/assistant", label: "AI Asistan", icon: BrainCircuit },
];

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand"><span>H</span> Hafıza</div>
        <nav aria-label="Ana menü">
          {navigation.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} className={({ isActive }) => isActive ? "nav-link active" : "nav-link"}>
              <Icon size={19} />{label}
            </NavLink>
          ))}
        </nav>
        <button className="settings-button"><Settings size={18} /> Ayarlar</button>
      </aside>
      <main className="main-content">{children}</main>
    </div>
  );
}
