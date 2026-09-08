import { useEffect, useMemo, useState } from "react";
import { ArrowRight, BookOpen, CalendarDays, Clock3, Search, Video } from "lucide-react";
import { searchLibraryUseCase } from "../app/dependencies";
import type { LibrarySource, LibrarySourceType } from "../domain/library/LibrarySource";

type LibraryTab = "all" | LibrarySourceType;
type DateFilter = "all" | "today" | "last-two-days";

export function LibraryPage() {
  const [activeTab, setActiveTab] = useState<LibraryTab>("all");
  const [sources, setSources] = useState<LibrarySource[]>([]);
  const [query, setQuery] = useState("");
  const [dateFilter, setDateFilter] = useState<DateFilter>("all");
  const [page, setPage] = useState(1);
  const pageSize = 6;

  useEffect(() => { void loadTab(activeTab); }, [activeTab]);
  async function loadTab(tab: LibraryTab) { setSources(await searchLibraryUseCase.getAll(tab === "all" ? undefined : tab)); }
  const filteredSources = useMemo(() => sources.filter((source) => {
    const matchesQuery = `${source.title} ${source.summary}`.toLocaleLowerCase("tr").includes(query.toLocaleLowerCase("tr"));
    const matchesDate = dateFilter === "all" || dateFilter === "today" && source.createdAtLabel.startsWith("Bugün") || dateFilter === "last-two-days" && ["Bugün", "Dün", "2 gün önce"].some((label) => source.createdAtLabel.startsWith(label));
    return matchesQuery && matchesDate;
  }), [sources, query, dateFilter]);
  const totalPages = Math.max(1, Math.ceil(filteredSources.length / pageSize));
  const visibleSources = filteredSources.slice((page - 1) * pageSize, page * pageSize);
  useEffect(() => { setPage(1); }, [query, dateFilter, activeTab]);

  return <div className="page library-page">
    <header className="library-header"><div><p className="eyebrow">KİŞİSEL ARŞİV</p><h1>Kaynaklarım</h1><p>Kaydettiğin video ve makaleleri bul, filtrele ve incele.</p></div><div className="source-count"><strong>{filteredSources.length}</strong><span>eşleşen kaynak</span></div></header>
    <section className="library-filters"><label className="library-search"><Search size={18}/><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Başlık veya içerikte ara…"/></label><label className="date-filter"><CalendarDays size={17}/><select value={dateFilter} onChange={(event) => setDateFilter(event.target.value as DateFilter)}><option value="all">Tüm tarihler</option><option value="today">Bugün</option><option value="last-two-days">Son 2 gün</option></select></label></section>
    <div className="library-toolbar"><div className="library-tabs">{(["all", "youtube", "article"] as LibraryTab[]).map((tab) => <button key={tab} className={activeTab === tab ? "active" : ""} onClick={() => setActiveTab(tab)}>{tab === "all" ? "Tümü" : tab === "youtube" ? "YouTube" : "Makaleler"}</button>)}</div><span>{query || dateFilter !== "all" ? `${filteredSources.length} eşleşme bulundu` : `${filteredSources.length} kaynak gösteriliyor`}</span></div>
    <div className="library-grid">{visibleSources.map((source) => <article className="library-card" key={source.id}><div className={`library-card-icon ${source.sourceType}`}>{source.sourceType === "youtube" ? <Video size={21}/> : <BookOpen size={21}/>}</div><div className="library-card-body"><div className="library-card-meta"><span>{source.sourceType === "youtube" ? "YOUTUBE" : "MAKALE"}</span><span><Clock3 size={13}/>{source.durationLabel}</span></div><h2>{source.title}</h2><p>{source.summary}</p><footer><span>{source.createdAtLabel}</span><button>Kaynağı aç <ArrowRight size={15}/></button></footer></div></article>)}</div>
    {visibleSources.length === 0 && <div className="empty-state">Bu filtrelerle eşleşen bir kaynak bulunamadı.</div>}
    {filteredSources.length > 0 && <div className="pagination-controls"><button disabled={page === 1} onClick={() => setPage((value) => value - 1)}>Önceki</button><span>{page} / {totalPages}</span><button disabled={page === totalPages} onClick={() => setPage((value) => value + 1)}>Sonraki</button></div>}
  </div>;
}
