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
  { name: 'FAQs', icon: '❓', description: 'Help & support', component: (token: string, toast: ReturnType<typeof useToast>) => <Faqs token={token} toast={toast} /> },
  { name: 'Dashboard', icon: '📊', description: 'Usage analytics', component: (token: string, toast: ReturnType<typeof useToast>) => <Analytics token={token} toast={toast} /> },
  { name: 'Practice', icon: '✏️', description: 'Practice exams', component: (token: string, toast: ReturnType<typeof useToast>) => <ExamHub token={token} toast={toast} /> },
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
    <div className="min-h-screen flex flex-col">
      {/* Top Navigation Bar */}
      <header className="app-header">
        <div className="max-w-6xl mx-auto px-4">
          <div className="flex items-center justify-between h-14">
            
            {/* Logo & Brand */}
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-xl flex items-center justify-center">
                <span className="text-lg">📘</span>
              </div>
              <div className="flex items-center gap-2">
                <h1 className="text-base font-bold text-slate-800">Smart Study</h1>
                <span className="hidden sm:inline-flex items-center gap-1 text-[10px] text-cyan-600 font-semibold bg-cyan-50 px-2 py-0.5 rounded-full">
                  🔒 Exam Safe
                </span>
              </div>
            </div>

            {/* Navigation Tabs - Desktop */}
            <nav className="hidden md:flex items-center">
              <div className="flex gap-1">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    onClick={() => setPage(i)}
                    className={`nav-tab ${i === page ? 'active' : ''}`}
                  >
                    <span>{p.icon}</span>
                    <span>{p.name}</span>
                  </button>
                ))}
              </div>
            </nav>

            {/* Mobile Navigation */}
            <nav className="md:hidden">
              <div className="flex gap-0.5 bg-slate-100 rounded-lg p-0.5">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    onClick={() => setPage(i)}
                    className={`nav-tab-mobile ${i === page ? 'active' : ''}`}
                  >
                    <span>{p.icon}</span>
                    <span className="text-[10px]">{p.name}</span>
                  </button>
                ))}
              </div>
            </nav>

            {/* Sign Out */}
            <button
              onClick={handleLogout}
              className="hidden md:flex items-center gap-1.5 px-3 py-1.5 text-slate-500 hover:text-red-600 hover:bg-red-50 rounded-lg text-sm transition-colors"
            >
              <span>👋</span>
              <span>Sign Out</span>
            </button>
          </div>
        </div>
      </header>

      <ToastContainer toasts={toast.toasts} onClose={toast.removeToast} />
      
      {/* Page Content */}
      <main className="flex-1 py-4">
        {PAGES[page].component(token || "", toast)}
      </main>
      
      {/* Footer */}
      <footer className="app-footer">
        <span>🔒</span>
        <span>All answers are from uploaded syllabus materials only</span>
      </footer>
    </div>
  );
}

export default App;
