import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';

interface Session {
  sessionId: string;
  lastMessage: string;
  timestamp: string;
}

interface SessionsListProps {
  onSelectSession: (sessionId: string) => void;
}

export default function SessionsList({ onSelectSession }: SessionsListProps) {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(true);

  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

  useEffect(() => {
    fetchSessions();
  }, []);

  const fetchSessions = async () => {
    try {
      setLoading(true);
      const response = await fetch(`${API_BASE}/api/chat/sessions?limit=10`);
      const data = await response.json();
      
      if (data.status === 'success') {
        setSessions(data.sessions || []);
      }
    } catch (error) {
      console.error('Error fetching sessions:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatTimestamp = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      if (isNaN(date.getTime())) {
        return 'Recently';
      }
      const now = new Date();
      const diff = now.getTime() - date.getTime();
      const hours = Math.floor(diff / (1000 * 60 * 60));
      
      if (hours < 1) return 'Just now';
      if (hours < 24) return `${hours}h ago`;
      const days = Math.floor(hours / 24);
      if (days === 1) return 'Yesterday';
      return `${days} days ago`;
    } catch (error) {
      return 'Recently';
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-4">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  if (sessions.length === 0) {
    return (
      <p className="text-center text-gray-500 py-4">No previous sessions</p>
    );
  }

  return (
    <div className="space-y-2 max-h-64 overflow-y-auto">
      {sessions.map((session, index) => (
        <motion.button
          key={`${session.sessionId}-${index}`}
          onClick={() => onSelectSession(session.sessionId)}
          whileHover={{ scale: 1.02 }}
          className="w-full text-left bg-gray-50 hover:bg-indigo-50 rounded-lg p-3 transition-all border border-gray-200 hover:border-indigo-300"
        >
          <p className="text-sm text-gray-900 font-medium truncate">{session.lastMessage}</p>
          <p className="text-xs text-gray-500 mt-1">{formatTimestamp(session.timestamp)}</p>
        </motion.button>
      ))}
    </div>
  );
}
