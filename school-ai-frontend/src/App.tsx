import ChatBot from './ChatBot';
import FileUpload from './FileUpload';
import Faqs from './Faqs';
import Analytics from './Analytics';
import ExamHub from './pages/ExamHub';
import { ToastContainer } from './components/Toast';
import { useToast } from './hooks/useToast';
import { useState } from "react";

const PAGES = [
  { name: 'Study', icon: '📚', description: 'Ask questions from your syllabus', component: (token: string, toast: ReturnType<typeof useToast>) => <ChatBot token={token} toast={toast} /> },
  { name: 'Upload', icon: '📤', description: 'Add syllabus materials', component: (token: string, toast: ReturnType<typeof useToast>) => <FileUpload token={token} toast={toast} /> },
  { name: 'FAQs', icon: '💡', description: 'Common questions answered', component: (token: string, toast: ReturnType<typeof useToast>) => <Faqs token={token} toast={toast} /> },
  { name: 'Dashboard', icon: '📊', description: 'View usage analytics', component: (token: string, toast: ReturnType<typeof useToast>) => <Analytics token={token} toast={toast} /> },
  { name: 'Practice', icon: '✏️', description: 'Take practice exams', component: (token: string, toast: ReturnType<typeof useToast>) => <ExamHub token={token} toast={toast} /> },
];

function App() {
  const [page, setPage] = useState(0);
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('jwt') || null);
  const toast = useToast();

  const handleLogout = () => {
    localStorage.removeItem('jwt');
    setToken(null);
    setPage(0);
  };

  return (
    <div className="min-h-screen">
      {/* Top Navigation Bar */}
      <header className="bg-white/90 backdrop-blur-md border-b border-slate-200/80 sticky top-0 z-50 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex items-center justify-between h-16">
            
            {/* Logo & Brand */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-xl flex items-center justify-center shadow-lg shadow-cyan-500/25">
                <span className="text-xl">📘</span>
              </div>
              <div>
                <h1 className="text-lg font-bold text-slate-900">Smart Study</h1>
                <span className="text-[10px] text-cyan-600 font-medium">🔒 Exam Safe</span>
              </div>
            </div>

            {/* Navigation Tabs */}
            <nav className="hidden md:flex items-center">
              <div className="flex bg-slate-100/80 rounded-xl p-1 gap-1">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    onClick={() => setPage(i)}
                    title={p.description}
                    className={`flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 ${
                      i === page
                        ? 'bg-white text-cyan-700 shadow-md'
                        : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
                    }`}
                  >
                    <span className="text-base">{p.icon}</span>
                    <span>{p.name}</span>
                  </button>
                ))}
              </div>
            </nav>

            {/* Mobile Navigation */}
            <nav className="md:hidden flex items-center">
              <div className="flex bg-slate-100/80 rounded-xl p-1 gap-0.5">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    onClick={() => setPage(i)}
                    title={p.description}
                    className={`flex items-center justify-center w-10 h-10 rounded-lg text-lg transition-all duration-200 ${
                      i === page
                        ? 'bg-white text-cyan-700 shadow-md'
                        : 'text-slate-500 hover:text-slate-900'
                    }`}
                  >
                    {p.icon}
                  </button>
                ))}
              </div>
            </nav>

            {/* User Actions */}
            <div className="flex items-center gap-2">
              <button
                onClick={handleLogout}
                className="flex items-center gap-2 px-3 py-2 text-slate-500 hover:text-red-600 hover:bg-red-50 rounded-lg text-sm font-medium transition-colors"
                title="Sign out"
              >
                <span>👋</span>
                <span className="hidden sm:inline">Sign Out</span>
              </button>
            </div>
          </div>
        </div>
        
        {/* Mobile Page Title */}
        <div className="md:hidden border-t border-slate-100 bg-slate-50/50 px-4 py-2">
          <div className="flex items-center gap-2">
            <span className="text-lg">{PAGES[page].icon}</span>
            <div>
              <p className="text-sm font-semibold text-slate-800">{PAGES[page].name}</p>
              <p className="text-xs text-slate-500">{PAGES[page].description}</p>
            </div>
          </div>
        </div>
      </header>

      <ToastContainer toasts={toast.toasts} onClose={toast.removeToast} />
      
      {/* Page Content */}
      <main className="py-6">
        {PAGES[page].component(token || "", toast)}
      </main>
      
      {/* Footer */}
      <footer className="py-3 text-center">
        <p className="text-xs text-slate-400">
          Smart Study — Making learning simple
        </p>
      </footer>
    </div>
  );
}

export default App;
