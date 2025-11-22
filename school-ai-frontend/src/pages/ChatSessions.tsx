import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';

interface Session {
  sessionId: string;
  lastMessage: string;
  timestamp: string;
}

interface ChatSessionsProps {
  token: string;
  toast: any;
  onSelectSession: (sessionId: string) => void;
}

export default function ChatSessions({ token, toast, onSelectSession }: ChatSessionsProps) {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(true);

  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

  useEffect(() => {
    fetchSessions();
  }, []);

  const fetchSessions = async () => {
    try {
      setLoading(true);
      const response = await fetch(`${API_BASE}/api/chat/sessions?limit=50`);
      const data = await response.json();
      
      if (data.status === 'success') {
        setSessions(data.sessions || []);
      } else {
        toast.error('Failed to load sessions');
      }
    } catch (error) {
      console.error('Error fetching sessions:', error);
      toast.error('Failed to load chat sessions');
    } finally {
      setLoading(false);
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    if (days < 7) return `${days} days ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="bg-white rounded-2xl shadow-xl p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-900">My Chat Sessions</h2>
          <button
            onClick={fetchSessions}
            className="px-4 py-2 bg-indigo-100 text-indigo-600 rounded-lg hover:bg-indigo-200 transition-colors"
          >
            Refresh
          </button>
        </div>

        {sessions.length === 0 ? (
          <div className="text-center py-12">
            <svg className="w-16 h-16 text-gray-300 mx-auto mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
            <p className="text-gray-500 text-lg">No chat sessions yet</p>
            <p className="text-gray-400 text-sm mt-2">Start chatting to create your first session</p>
          </div>
        ) : (
          <div className="space-y-3">
            {sessions.map((session) => (
              <motion.button
                key={session.sessionId}
                onClick={() => onSelectSession(session.sessionId)}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
                className="w-full text-left bg-gray-50 hover:bg-indigo-50 rounded-xl p-4 transition-all duration-200 border border-gray-200 hover:border-indigo-300"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <p className="text-gray-900 font-medium truncate">{session.lastMessage}</p>
                    <p className="text-sm text-gray-500 mt-1">{formatTimestamp(session.timestamp)}</p>
                  </div>
                  <svg className="w-5 h-5 text-gray-400 ml-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                  </svg>
                </div>
              </motion.button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
