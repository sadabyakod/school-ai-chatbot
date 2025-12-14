import ChatBot from './ChatBot';
import FileUpload from './FileUpload';
import Faqs from './Faqs';
import Analytics from './Analytics';
import ExamHub from './pages/ExamHub';
import { ToastContainer } from './components/Toast';
import { useToast } from './hooks/useToast';
import { useState } from "react";

const PAGES = [
  { name: 'Chat', icon: '💬', component: (token: string, toast: ReturnType<typeof useToast>) => <ChatBot token={token} toast={toast} /> },
  { name: 'Upload', icon: '📁', component: (token: string, toast: ReturnType<typeof useToast>) => <FileUpload token={token} toast={toast} /> },
  { name: 'FAQs', icon: '❓', component: (token: string, toast: ReturnType<typeof useToast>) => <Faqs token={token} toast={toast} /> },
  { name: 'Analytics', icon: '📊', component: (token: string, toast: ReturnType<typeof useToast>) => <Analytics token={token} toast={toast} /> },
  { name: 'Exams', icon: '📝', component: (token: string, toast: ReturnType<typeof useToast>) => <ExamHub token={token} toast={toast} /> },
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
    <div className="min-h-screen bg-slate-50">
      {/* Top Navigation Bar */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex items-center justify-between h-16">
            
            {/* Logo */}
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 bg-indigo-600 rounded-xl flex items-center justify-center shadow-sm">
                <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3zM3.31 9.397L5 10.12v4.102a8.969 8.969 0 00-1.05-.174 1 1 0 01-.89-.89 11.115 11.115 0 01.25-3.762zM9.3 16.573A9.026 9.026 0 007 14.935v-3.957l1.818.78a3 3 0 002.364 0l5.508-2.361a11.026 11.026 0 01.25 3.762 1 1 0 01-.89.89 8.968 8.968 0 00-5.35 2.524 1 1 0 01-1.4 0zM6 18a1 1 0 001-1v-2.065a8.935 8.935 0 00-2-.712V17a1 1 0 001 1z" />
                </svg>
              </div>
              <div>
                <h1 className="text-lg font-bold text-slate-900">Smart Study</h1>
                <p className="text-xs text-slate-500 -mt-0.5 hidden sm:block">AI-Powered Learning</p>
              </div>
            </div>

            {/* Navigation Tabs */}
            <nav className="flex items-center">
              <div className="flex bg-slate-100 rounded-lg p-1 gap-1">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    onClick={() => setPage(i)}
                    className={`flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-all duration-200 ${
                      i === page
                        ? 'bg-white text-indigo-600 shadow-sm'
                        : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                    }`}
                  >
                    <span className="text-sm">{p.icon}</span>
                    <span className="hidden md:inline">{p.name}</span>
                  </button>
                ))}
              </div>
            </nav>

            {/* Logout Button */}
            <button
              onClick={handleLogout}
              className="flex items-center gap-2 px-3 py-2 text-slate-500 hover:text-red-600 hover:bg-red-50 rounded-lg text-sm font-medium transition-colors"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              <span className="hidden sm:inline">Logout</span>
            </button>
          </div>
        </div>
      </header>

      <ToastContainer toasts={toast.toasts} onClose={toast.removeToast} />
      
      {/* Page Content */}
      <main className="py-6">
        {PAGES[page].component(token || "", toast)}
      </main>
    </div>
  );
}

export default App;
