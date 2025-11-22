import React, { useState, useEffect, useRef } from "react";
import { sendChat, ApiException } from "./api";
import { motion } from "framer-motion";
import { useToast } from "./hooks/useToast";
import SessionsList from "./components/SessionsList";

interface Message {
  sender: "user" | "bot";
  text: string;
  timestamp: Date;
  id?: number;
  followUpQuestion?: string;
}

interface ChatHistory {
  id: number;
  message: string;
  reply: string;
  timestamp: string;
  contextCount: number;
}

const userAvatar = (
  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-bold shadow-lg">
    <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
      <path fillRule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clipRule="evenodd" />
    </svg>
  </div>
);

const botAvatar = (
  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 flex items-center justify-center text-white font-bold shadow-lg animate-pulse-slow">
    <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
      <path d="M2 5a2 2 0 012-2h7a2 2 0 012 2v4a2 2 0 01-2 2H9l-3 3v-3H4a2 2 0 01-2-2V5z" />
      <path d="M15 7v2a4 4 0 01-4 4H9.828l-1.766 1.767c.28.149.599.233.938.233h2l3 3v-3h2a2 2 0 002-2V9a2 2 0 00-2-2h-1z" />
    </svg>
  </div>
);

const suggestedQuestions = [
  "What subjects are available?",
  "Tell me about the curriculum",
  "How can I improve my grades?",
  "What are the school timings?"
];

const ChatBot: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(true);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [showSessions, setShowSessions] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    // Get most recent session or create new one
    fetchMostRecentSession();
  }, []);

  const fetchMostRecentSession = async () => {
    try {
      const response = await fetch(`${API_BASE}/api/chat/most-recent-session`);
      
      // Handle server errors gracefully
      if (!response.ok) {
        throw new Error(`Server error: ${response.status}`);
      }
      
      const data = await response.json();
      
      if (data.status === 'success' && data.sessionId) {
        setSessionId(data.sessionId);
        // Load history for this session
        await loadChatHistory(data.sessionId);
      } else {
        // Show welcome message if no session
        const welcomeMessage: Message = {
          sender: "bot",
          text: "Welcome To The Smart Neurozic AI Chat Bot...!!",
          timestamp: new Date()
        };
        setMessages([welcomeMessage]);
      }
    } catch (error) {
      console.error('Error fetching session:', error);
      // Fallback to basic mode without session history
      const welcomeMessage: Message = {
        sender: "bot",
        text: "Welcome To The Smart Neurozic AI Chat Bot...!!",
        timestamp: new Date()
      };
      setMessages([welcomeMessage]);
      setShowSessions(false); // Disable session features
    }
  };

  const loadChatHistory = async (sid: string) => {
    try {
      const response = await fetch(`${API_BASE}/api/chat/history?sessionId=${sid}&limit=50`);
      const data = await response.json();
      
      if (data.status === 'success' && data.messages) {
        const history: Message[] = data.messages.flatMap((msg: ChatHistory) => [
          {
            sender: 'user' as const,
            text: msg.message,
            timestamp: new Date(msg.timestamp),
            id: msg.id
          },
          {
            sender: 'bot' as const,
            text: msg.reply,
            timestamp: new Date(msg.timestamp),
            id: msg.id
          }
        ]).reverse();
        
        setMessages(history.length > 0 ? history : [{
          sender: "bot",
          text: "Welcome back! Continue our conversation...",
          timestamp: new Date()
        }]);
      }
    } catch (error) {
      console.error('Error loading history:', error);
    }
  };

  const handleSend = async (text?: string) => {
    const messageText = text || input;
    if (!messageText.trim() || loading) return;
    
    const userMessage: Message = { 
      sender: "user", 
      text: messageText,
      timestamp: new Date()
    };
    setMessages((msgs) => [...msgs, userMessage]);
    setInput("");
    setShowSuggestions(false);
    setLoading(true);
    
    try {
      const response = await fetch(`${API_BASE}/api/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          question: messageText,
          sessionId: sessionId
        })
      });

      const data = await response.json();

      if (data.status === 'success') {
        // Update session ID if it changed or was created
        if (data.sessionId && data.sessionId !== sessionId) {
          setSessionId(data.sessionId);
        }

        const botMessage: Message = {
          sender: "bot",
          text: data.reply || "I apologize, but I couldn't generate a response. Please try again.",
          timestamp: new Date(data.timestamp),
          followUpQuestion: data.followUpQuestion
        };
        setMessages((msgs) => [...msgs, botMessage]);
      } else {
        throw new Error(data.message || 'Failed to get response');
      }
    } catch (err) {
      const error = err as ApiException;
      const errorMessage: Message = {
        sender: "bot",
        text: "😔 Oops! I couldn't reach the server. Please check your connection or try again.",
        timestamp: new Date()
      };
      setMessages((msgs) => [...msgs, errorMessage]);
      toast.error(error.message || "Failed to get response from AI");
    } finally {
      setLoading(false);
    }
  };

  const handleNewSession = () => {
    setSessionId(null);
    setMessages([{
      sender: "bot",
      text: "New conversation started! How can I help you today?",
      timestamp: new Date()
    }]);
    setShowSuggestions(true);
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  };

  const messageVariants = {
    hidden: { opacity: 0, y: 10 },
    visible: { opacity: 1, y: 0 },
  };

  const suggestionVariants = {
    hover: { scale: 1.05, boxShadow: "0px 4px 10px rgba(0, 0, 0, 0.2)" },
  };

  return (
    <div className="w-full max-w-4xl mx-auto mt-2 sm:mt-6 rounded-3xl shadow-2xl flex flex-col h-[90vh] max-h-[800px] bg-white overflow-hidden border border-gray-200">
      {/* Header */}
      <div className="bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white px-6 py-4 flex items-center justify-between shadow-lg">
        <div className="flex items-center gap-3">
          <div className="relative">
            {botAvatar}
            <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-400 border-2 border-white rounded-full"></span>
          </div>
          <div>
            <h2 className="font-bold text-xl tracking-wide">School AI Assistant</h2>
            <p className="text-xs opacity-90 flex items-center gap-1">
              <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse"></span>
              {sessionId ? `Session: ${sessionId.substring(0, 8)}...` : 'Ready to help'}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handleNewSession}
            className="px-4 py-2 bg-white text-indigo-600 rounded-lg hover:bg-indigo-50 transition-colors text-sm font-medium"
            title="Start New Chat"
          >
            New Chat
          </button>
          <button
            onClick={() => setShowSessions(!showSessions)}
            className="px-4 py-2 bg-white text-indigo-600 rounded-lg hover:bg-indigo-50 transition-colors text-sm font-medium"
            title="View Chat History"
          >
            History
          </button>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 sm:p-6 bg-gradient-to-b from-gray-50 to-white">
        <div className="space-y-4">
          {messages.map((msg, idx) => (
            <motion.div
              key={idx}
              className={`flex items-end gap-3 ${msg.sender === "user" ? "justify-end" : "justify-start"}`}
              initial="hidden"
              animate="visible"
              variants={messageVariants}
              transition={{ duration: 0.3 }}
            >
              {msg.sender === "bot" && <div className="flex-shrink-0 mb-1">{botAvatar}</div>}
              <div className={`flex flex-col ${msg.sender === "user" ? "items-end" : "items-start"} max-w-[75vw] sm:max-w-xl`}>
                <div
                  className={`px-5 py-3 rounded-2xl text-base break-words shadow-md transition-all duration-300 hover:shadow-lg ${
                    msg.sender === "user"
                      ? "bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-br-sm"
                      : "bg-white text-gray-800 border border-gray-200 rounded-bl-sm"
                  }`}
                >
                  {msg.text}
                </div>
                {msg.sender === "bot" && msg.followUpQuestion && (
                  <motion.button
                    onClick={() => handleSend(msg.followUpQuestion!)}
                    className="mt-2 px-4 py-2 bg-gradient-to-r from-purple-100 to-pink-100 hover:from-purple-200 hover:to-pink-200 text-purple-900 rounded-xl text-sm border border-purple-300 transition-all duration-200 text-left shadow-sm hover:shadow-md"
                    whileHover={{ scale: 1.02 }}
                    whileTap={{ scale: 0.98 }}
                  >
                    <span className="font-medium">💡 {msg.followUpQuestion}</span>
                  </motion.button>
                )}
                <span className="text-xs text-gray-400 mt-1 px-2">{formatTime(msg.timestamp)}</span>
              </div>
              {msg.sender === "user" && <div className="flex-shrink-0 mb-1">{userAvatar}</div>}
            </motion.div>
          ))}

          {loading && (
            <motion.div
              className="flex items-end gap-3 justify-start"
              initial="hidden"
              animate="visible"
              variants={messageVariants}
              transition={{ duration: 0.3 }}
            >
              <div className="flex-shrink-0 mb-1">{botAvatar}</div>
              <div className="bg-white border border-gray-200 px-5 py-3 rounded-2xl rounded-bl-sm shadow-md">
                <div className="flex gap-1">
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: "0ms" }}></span>
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: "150ms" }}></span>
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: "300ms" }}></span>
                </div>
              </div>
            </motion.div>
          )}
          
          {/* Suggested Questions */}
          {showSuggestions && messages.length === 1 && (
            <motion.div
              className="mt-6"
              initial="hidden"
              animate="visible"
              variants={messageVariants}
              transition={{ duration: 0.5 }}
            >
              <p className="text-sm text-gray-500 mb-3 text-center font-medium">💡 Try asking:</p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {suggestedQuestions.map((q, i) => (
                  <motion.button
                    key={i}
                    onClick={() => handleSend(q)}
                    className="bg-gradient-to-r from-purple-50 to-pink-50 hover:from-purple-100 hover:to-pink-100 text-gray-700 px-4 py-3 rounded-xl text-sm border border-purple-200 transition-all duration-200 text-left"
                    whileHover="hover"
                    variants={suggestionVariants}
                  >
                    <span className="font-medium">{q}</span>
                  </motion.button>
                ))}
              </div>
            </motion.div>
          )}
          
          {/* Show Sessions Modal */}
          {showSessions && (
            <div className="mt-6 bg-white rounded-xl border-2 border-indigo-200 p-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-bold text-gray-900">Recent Sessions</h3>
                <button
                  onClick={() => setShowSessions(false)}
                  className="text-gray-500 hover:text-gray-700"
                >
                  ✕
                </button>
              </div>
              <SessionsList 
                onSelectSession={(sid: string) => {
                  setSessionId(sid);
                  loadChatHistory(sid);
                  setShowSessions(false);
                }}
              />
            </div>
          )}
          
          <div ref={messagesEndRef} />
        </div>
      </div>

      {/* Input Area */}
      <div className="bg-white border-t border-gray-200 p-4 sm:p-5">
        <div className="flex gap-2 items-end">
          <div className="flex-1 relative">
            <textarea
              className="w-full border-2 border-gray-300 rounded-2xl px-5 py-3 focus:outline-none focus:border-purple-500 focus:ring-2 focus:ring-purple-200 text-base transition-all duration-200 pr-12 resize-none min-h-[56px] max-h-[200px]"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  handleSend();
                }
              }}
              placeholder="Type your message..."
              disabled={loading}
              rows={1}
              style={{
                height: 'auto',
                overflowY: input.split('\n').length > 3 ? 'auto' : 'hidden'
              }}
              onInput={(e) => {
                const target = e.target as HTMLTextAreaElement;
                target.style.height = 'auto';
                target.style.height = Math.min(target.scrollHeight, 200) + 'px';
              }}
            />
            <div className="absolute right-3 top-4">
              <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <button
            className="bg-gradient-to-r from-purple-500 to-pink-600 text-white p-4 rounded-2xl hover:from-purple-600 hover:to-pink-700 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 hover:shadow-lg hover:scale-105 active:scale-95"
            onClick={() => handleSend()}
            disabled={loading || !input.trim()}
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
            </svg>
          </button>
        </div>
        <div className="mt-2 text-xs text-gray-400 text-center">
          Powered by Neurozic AI • Press Enter to send • Shift+Enter for new line
        </div>
      </div>

      {/* Animations */}
      <style>{`
        @keyframes slide-in {
          from {
            opacity: 0;
            transform: translateY(10px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        .animate-slide-in {
          animation: slide-in 0.3s ease-out;
        }
        @keyframes fade-in {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        .animate-fade-in {
          animation: fade-in 0.5s ease-out;
        }
        @keyframes pulse-slow {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.8; }
        }
        .animate-pulse-slow {
          animation: pulse-slow 3s ease-in-out infinite;
        }
        /* Custom scrollbar */
        .overflow-y-auto::-webkit-scrollbar {
          width: 6px;
        }
        .overflow-y-auto::-webkit-scrollbar-track {
          background: #f1f1f1;
          border-radius: 10px;
        }
        .overflow-y-auto::-webkit-scrollbar-thumb {
          background: linear-gradient(to bottom, #a855f7, #ec4899);
          border-radius: 10px;
        }
        .overflow-y-auto::-webkit-scrollbar-thumb:hover {
          background: linear-gradient(to bottom, #9333ea, #db2777);
        }
      `}</style>
    </div>
  );
};

export default ChatBot;
