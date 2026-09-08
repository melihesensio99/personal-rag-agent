import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { AssistantPage } from "../pages/AssistantPage";
import { LibraryPage } from "../pages/LibraryPage";
import { QuickSummaryPage } from "../pages/QuickSummaryPage";

export function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<Navigate to="/quick-summary" replace />} />
        <Route path="/quick-summary" element={<QuickSummaryPage />} />
        <Route path="/library" element={<LibraryPage />} />
        <Route path="/assistant" element={<AssistantPage />} />
      </Routes>
    </AppShell>
  );
}
