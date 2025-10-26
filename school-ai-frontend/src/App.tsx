import ChatBot from './ChatBot';
import FileUpload from './FileUpload';
import Faqs from './Faqs';
import Analytics from './Analytics';
import React, { useState } from "react";

const PAGES = [
  { name: 'Chat', component: (token: string) => <ChatBot token={token} /> },
  { name: 'File Upload', component: (token: string) => <FileUpload token={token} /> },
  { name: 'FAQs', component: (token: string) => <Faqs token={token} /> },
  { name: 'Analytics', component: (token: string) => <Analytics token={token} /> },
];

function App() {
  const [page, setPage] = useState(0);
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('jwt') || null);

  const handleLogout = () => {
    localStorage.removeItem('jwt');
    setToken(null);
    setPage(0); // Reset to first page
  };

  // Authentication temporarily bypassed; always show main app

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50">
      <nav className="bg-white shadow-lg border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-8">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center shadow-lg">
                  <svg className="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3zM3.31 9.397L5 10.12v4.102a8.969 8.969 0 00-1.05-.174 1 1 0 01-.89-.89 11.115 11.115 0 01.25-3.762zM9.3 16.573A9.026 9.026 0 007 14.935v-3.957l1.818.78a3 3 0 002.364 0l5.508-2.361a11.026 11.026 0 01.25 3.762 1 1 0 01-.89.89 8.968 8.968 0 00-5.35 2.524 1 1 0 01-1.4 0zM6 18a1 1 0 001-1v-2.065a8.935 8.935 0 00-2-.712V17a1 1 0 001 1z" />
                  </svg>
                </div>
                <h1 className="text-2xl font-bold bg-gradient-to-r from-indigo-600 to-pink-600 bg-clip-text text-transparent">
                  School AI Hub
                </h1>
              </div>
              <div className="flex gap-2">
                {PAGES.map((p, i) => (
                  <button
                    key={p.name}
                    className={`px-4 py-2 rounded-lg font-medium transition-all duration-200 ${
                      i === page
                        ? 'bg-gradient-to-r from-indigo-500 to-purple-600 text-white shadow-md scale-105'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200 hover:scale-105'
                    }`}
                    onClick={() => setPage(i)}
                  >
                    {p.name}
                  </button>
                ))}
              </div>
            </div>
            <button 
              className="text-red-500 hover:text-red-700 font-medium px-4 py-2 hover:bg-red-50 rounded-lg transition-all duration-200" 
              onClick={handleLogout}
            >
              Logout
            </button>
          </div>
        </div>
      </nav>
      <div className="py-4">
        {PAGES[page].component(token || "")}
      </div>
    </div>
  );
}

export default App;
