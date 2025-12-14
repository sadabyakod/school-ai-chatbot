import React, { useState, useEffect, useRef } from "react";
import { ApiException, API_URL } from "./api";
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

const syllabusQuestions = [
  { icon: "⚡", text: "Explain Ohm's Law with examples", subject: "Physics", chapter: "Current Electricity" },
  { icon: "🧮", text: "Derive the quadratic formula", subject: "Mathematics", chapter: "Algebra" },
  { icon: "🌱", text: "What is photosynthesis?", subject: "Biology", chapter: "Plant Physiology" },
  { icon: "⚗️", text: "Explain types of chemical bonds", subject: "Chemistry", chapter: "Chemical Bonding" },
];

const ChatBot: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ toast }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [showSessions, setShowSessions] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  const API_BASE = API_URL;

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    fetchMostRecentSession();
  }, []);

  const fetchMostRecentSession = async () => {
    try {
      const response = await fetch(`${API_BASE}/api/chat/most-recent-session`);
      if (!response.ok) throw new Error(`Server error: ${response.status}`);
      const data = await response.json();
      
      if (data.status === 'success' && data.sessionId) {
        setSessionId(data.sessionId);
        await loadChatHistory(data.sessionId);
      }
    } catch (error) {
      console.error('Error fetching session:', error);
    }
  };

  const loadChatHistory = async (sid: string) => {
    try {
      const response = await fetch(`${API_BASE}/api/chat/history?sessionId=${sid}&limit=50`);
      const data = await response.json();
      
      if (data.status === 'success' && data.messages) {
        const history: Message[] = data.messages.flatMap((msg: ChatHistory) => [
          { sender: 'user' as const, text: msg.message, timestamp: new Date(msg.timestamp), id: msg.id },
          { sender: 'bot' as const, text: msg.reply, timestamp: new Date(msg.timestamp), id: msg.id }
        ]).reverse();
        setMessages(history);
      }
    } catch (error) {
      console.error('Error loading history:', error);
    }
  };

  const handleSend = async (text?: string) => {
    const messageText = text || input;
    if (!messageText.trim() || loading) return;
    
    const userMessage: Message = { sender: "user", text: messageText, timestamp: new Date() };
    setMessages((msgs) => [...msgs, userMessage]);
    setInput("");
    setLoading(true);
    
    if (inputRef.current) {
      inputRef.current.style.height = 'auto';
    }
    
    try {
      const response = await fetch(`${API_BASE}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ question: messageText, sessionId })
      });

      const data = await response.json();

      if (data.status === 'success') {
        if (data.sessionId && data.sessionId !== sessionId) {
          setSessionId(data.sessionId);
        }
        const botMessage: Message = {
          sender: "bot",
          text: data.reply || "I couldn't find an answer in the syllabus. Try rephrasing your question.",
          timestamp: new Date(data.timestamp),
          followUpQuestion: data.followUpQuestion
        };
        setMessages((msgs) => [...msgs, botMessage]);
      } else {
        throw new Error(data.message || 'Failed to get response');
      }
    } catch (err) {
      const error = err as ApiException;
      setMessages((msgs) => [...msgs, {
        sender: "bot",
        text: "😔 I couldn't process your question right now. Please check your connection and try again.",
        timestamp: new Date()
      }]);
      toast.error(error.message || "Couldn't get a response. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleNewSession = () => {
    setSessionId(null);
    setMessages([]);
    setShowSessions(false);
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  };

  const hasMessages = messages.length > 0;

  return (
    <div className="max-w-4xl mx-auto px-4">
      {/* Main Chat Card */}
      <div className="card overflow-hidden flex flex-col" style={{ height: 'calc(100vh - 180px)' }}>
        
        {/* Header */}
        <div className="bg-gradient-to-r from-cyan-50 to-teal-50 border-b border-cyan-100 px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-cyan-500 to-teal-600 flex items-center justify-center shadow-lg shadow-cyan-500/25">
                <span className="text-2xl">📚</span>
              </div>
              <div>
                <h1 className="text-xl font-bold text-slate-900">Study Assistant</h1>
                <p className="text-sm text-slate-600">Ask any question from your syllabus</p>
              </div>
            </div>
            
            <div className="flex items-center gap-2">
              <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-white/80 rounded-lg border border-slate-200 text-xs">
                <span className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse"></span>
                <span className="font-medium text-slate-700">Karnataka 2nd PUC</span>
              </div>
              <button
                onClick={handleNewSession}
                className="p-2.5 text-slate-500 hover:text-cyan-600 hover:bg-cyan-50 rounded-lg transition-colors"
                title="Start new conversation"
              >
                <span className="text-lg">✨</span>
              </button>
              <button
                onClick={() => setShowSessions(!showSessions)}
                className={`p-2.5 rounded-lg transition-colors ${showSessions ? 'text-cyan-600 bg-cyan-50' : 'text-slate-500 hover:text-cyan-600 hover:bg-cyan-50'}`}
                title="View chat history"
              >
                <span className="text-lg">📜</span>
              </button>
            </div>
          </div>
        </div>

        {/* Sessions Panel */}
        {showSessions && (
          <div className="border-b border-slate-200 bg-slate-50 p-4 max-h-48 overflow-y-auto">
            <SessionsList 
              onSelectSession={(sid: string) => {
                setSessionId(sid);
                loadChatHistory(sid);
                setShowSessions(false);
              }}
            />
          </div>
        )}

        {/* Messages Area */}
        <div className="flex-1 overflow-y-auto bg-gradient-to-b from-slate-50 to-white p-6">
          
          {/* Empty State */}
          {!hasMessages && !loading && (
            <div className="flex flex-col items-center justify-center h-full text-center">
              <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-cyan-100 to-teal-100 flex items-center justify-center mb-6">
                <span className="text-4xl">📖</span>
              </div>
              <h2 className="text-2xl font-bold text-slate-800 mb-2">What would you like to learn?</h2>
              <p className="text-slate-500 mb-2 max-w-md">
                Ask any question from your Karnataka 2nd PUC syllabus
              </p>
              <p className="text-xs text-cyan-600 font-medium mb-8 flex items-center gap-1">
                <span>🔒</span> All answers come directly from your syllabus materials
              </p>

              {/* Suggestion Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full max-w-lg">
                {syllabusQuestions.map((q, i) => (
                  <button
                    key={i}
                    onClick={() => handleSend(q.text)}
                    className="flex items-start gap-3 p-4 bg-white rounded-xl border border-slate-200 hover:border-cyan-300 hover:shadow-lg hover:shadow-cyan-100 transition-all text-left group"
                  >
                    <span className="text-2xl mt-0.5">{q.icon}</span>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-slate-800 group-hover:text-cyan-700 transition-colors line-clamp-2">{q.text}</p>
                      <p className="text-xs text-slate-400 mt-1">{q.subject} • {q.chapter}</p>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Chat Messages */}
          {hasMessages && (
            <div className="space-y-6">
              {messages.map((msg, idx) => (
                <div
                  key={idx}
                  className={`flex ${msg.sender === 'user' ? 'justify-end' : 'justify-start'} animate-fade-in-up`}
                >
                  <div className={`max-w-[85%] ${msg.sender === 'user' ? 'order-1' : ''}`}>
                    {/* Avatar + Message */}
                    <div className={`flex gap-3 ${msg.sender === 'user' ? 'flex-row-reverse' : ''}`}>
                      {/* Avatar */}
                      <div className={`flex-shrink-0 w-9 h-9 rounded-full flex items-center justify-center shadow-md ${
                        msg.sender === 'user' 
                          ? 'bg-slate-200' 
                          : 'bg-gradient-to-br from-cyan-500 to-teal-600'
                      }`}>
                        {msg.sender === 'user' ? (
                          <span className="text-base">🧑‍🎓</span>
                        ) : (
                          <span className="text-base">📘</span>
                        )}
                      </div>

                      {/* Message Content */}
                      <div className="flex-1">
                        {msg.sender === 'bot' && (
                          <div className="flex items-center gap-2 mb-1.5">
                            <span className="text-xs font-semibold text-cyan-700">From Your Syllabus</span>
                            <span className="text-xs text-slate-400">•</span>
                            <span className="text-xs text-emerald-600 flex items-center gap-0.5">
                              <span>✓</span> Verified
                            </span>
                          </div>
                        )}
                        <div
                          className={`px-4 py-3 rounded-2xl text-base leading-relaxed ${
                            msg.sender === 'user'
                              ? 'bg-slate-100 text-slate-800 rounded-br-md'
                              : 'message-bot rounded-bl-md shadow-sm'
                          }`}
                        >
                          <p className="whitespace-pre-wrap">{msg.text}</p>
                        </div>
                        
                        {/* Follow-up Question */}
                        {msg.sender === 'bot' && msg.followUpQuestion && (
                          <button
                            onClick={() => handleSend(msg.followUpQuestion!)}
                            className="mt-3 px-4 py-2.5 bg-cyan-50 hover:bg-cyan-100 text-cyan-700 rounded-xl text-sm border border-cyan-200 transition-colors flex items-center gap-2 group"
                          >
                            <span className="text-lg">💡</span>
                            <span className="group-hover:text-cyan-800">{msg.followUpQuestion}</span>
                          </button>
                        )}
                        
                        <span className="text-xs text-slate-400 mt-1.5 block">{formatTime(msg.timestamp)}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {/* Typing Indicator */}
              {loading && (
                <div className="flex justify-start animate-fade-in-up">
                  <div className="flex gap-3">
                    <div className="w-9 h-9 rounded-full bg-gradient-to-br from-cyan-500 to-teal-600 flex items-center justify-center shadow-md">
                      <span className="text-base">📘</span>
                    </div>
                    <div className="bg-white border border-cyan-200 px-5 py-3 rounded-2xl rounded-bl-md shadow-sm">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-slate-500">Searching syllabus</span>
                        <div className="flex gap-1">
                          <span className="w-1.5 h-1.5 bg-cyan-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></span>
                          <span className="w-1.5 h-1.5 bg-cyan-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></span>
                          <span className="w-1.5 h-1.5 bg-cyan-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>
          )}
        </div>

        {/* Input Area */}
        <div className="bg-white border-t border-slate-200 p-4">
          <div className="flex gap-3 items-end">
            <textarea
              ref={inputRef}
              className="flex-1 bg-slate-50 border border-slate-200 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-cyan-500 focus:border-transparent focus:bg-white text-base text-slate-800 placeholder-slate-400 resize-none min-h-[48px] max-h-[120px] transition-all"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  handleSend();
                }
              }}
              onInput={(e) => {
                const target = e.target as HTMLTextAreaElement;
                target.style.height = 'auto';
                target.style.height = Math.min(target.scrollHeight, 120) + 'px';
              }}
              placeholder="Ask a question from your syllabus... (e.g., What is Newton's second law?)"
              disabled={loading}
              rows={1}
            />
            <button
              className={`h-12 px-6 rounded-xl font-semibold flex items-center justify-center gap-2 transition-all ${
                input.trim() && !loading
                  ? 'bg-gradient-to-r from-cyan-500 to-teal-600 text-white hover:shadow-lg hover:shadow-cyan-500/25 hover:-translate-y-0.5'
                  : 'bg-slate-100 text-slate-400 cursor-not-allowed'
              }`}
              onClick={() => handleSend()}
              disabled={loading || !input.trim()}
            >
              {loading ? (
                <div className="w-5 h-5 border-2 border-slate-300 border-t-transparent rounded-full animate-spin" />
              ) : (
                <>
                  <span className="hidden sm:inline">Ask</span>
                  <span>📤</span>
                </>
              )}
            </button>
          </div>
          <p className="text-xs text-slate-400 text-center mt-3 flex items-center justify-center gap-2">
            <span>🔒</span>
            Answers come only from your uploaded syllabus
            <span>•</span>
            Press Enter to send
          </p>
        </div>
      </div>
    </div>
  );
};

export default ChatBot;
