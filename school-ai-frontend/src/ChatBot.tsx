import React, { useState, useEffect, useRef } from "react";
import { ApiException } from "./api";
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
  { icon: "⚡", text: "Explain Ohm's Law with examples", subject: "Physics" },
  { icon: "🧮", text: "Derive the quadratic formula", subject: "Mathematics" },
  { icon: "🌱", text: "What is photosynthesis?", subject: "Biology" },
  { icon: "⚗️", text: "Explain chemical bonding types", subject: "Chemistry" },
];

const ChatBot: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ toast }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [showSessions, setShowSessions] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

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
          text: data.reply || "I couldn't generate a response. Please try again.",
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
        text: "I couldn't reach the server. Please check your connection.",
        timestamp: new Date()
      }]);
      toast.error(error.message || "Failed to get response");
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
      <div className="bg-white rounded-2xl shadow-lg border border-slate-200 overflow-hidden flex flex-col" style={{ height: 'calc(100vh - 140px)' }}>
        
        {/* Header */}
        <div className="bg-white border-b border-slate-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg">
                <span className="text-2xl">📚</span>
              </div>
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold text-slate-900">Study Assistant</h1>
                <p className="text-sm text-slate-500">Your syllabus-based learning companion</p>
                <div className="flex items-center gap-2 mt-1">
                  <span className="inline-flex items-center gap-1 px-2 py-1 bg-blue-50 text-blue-700 rounded-full text-xs font-medium">
                    <span>🔒</span>
                    Exam Safe • Syllabus Only
                  </span>
                </div>
              </div>
            </div>
            
            <div className="flex items-center gap-2">
              <span className="hidden sm:inline-flex items-center gap-1.5 px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded-full text-xs font-medium">
                <span className="w-2 h-2 bg-emerald-500 rounded-full"></span>
                Karnataka 2nd PUC
              </span>
              <button
                onClick={handleNewSession}
                className="p-2.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                title="New Chat"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
              </button>
              <button
                onClick={() => setShowSessions(!showSessions)}
                className={`p-2.5 rounded-lg transition-colors ${showSessions ? 'text-indigo-600 bg-indigo-50' : 'text-slate-500 hover:text-indigo-600 hover:bg-indigo-50'}`}
                title="Chat History"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
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
        <div className="flex-1 overflow-y-auto bg-slate-50 p-6">
          
          {/* Empty State */}
          {!hasMessages && !loading && (
            <div className="flex flex-col items-center justify-center h-full text-center">
              <div className="w-16 h-16 rounded-2xl bg-indigo-100 flex items-center justify-center mb-6">
                <svg className="w-8 h-8 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                </svg>
              </div>
              <h2 className="text-2xl font-bold text-slate-900 mb-2">Ask any question from your syllabus</h2>
              <p className="text-slate-500 mb-8 max-w-md">
                Get instant, accurate answers based on Karnataka 2nd PUC curriculum
              </p>

              {/* Suggestion Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full max-w-lg">
                {syllabusQuestions.map((q, i) => (
                  <button
                    key={i}
                    onClick={() => handleSend(q.text)}
                    className="flex items-center gap-3 p-4 bg-white rounded-xl border border-slate-200 hover:border-indigo-300 hover:shadow-md transition-all text-left group"
                  >
                    <span className="text-xl">{q.icon}</span>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-slate-800 group-hover:text-indigo-600 transition-colors">{q.text}</p>
                      <p className="text-xs text-slate-400">{q.subject}</p>
                    </div>
                    <svg className="w-4 h-4 text-slate-300 group-hover:text-indigo-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
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
                  className={`flex ${msg.sender === 'user' ? 'justify-end' : 'justify-start'}`}
                >
                  <div className={`max-w-[80%] ${msg.sender === 'user' ? 'order-1' : ''}`}>
                    {/* Avatar + Message */}
                    <div className={`flex gap-3 ${msg.sender === 'user' ? 'flex-row-reverse' : ''}`}>
                      {/* Avatar */}
                      <div className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${
                        msg.sender === 'user' 
                          ? 'bg-indigo-600' 
                          : 'bg-gradient-to-br from-indigo-500 to-purple-600'
                      }`}>
                        {msg.sender === 'user' ? (
                          <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clipRule="evenodd" />
                          </svg>
                        ) : (
                          <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                          </svg>
                        )}
                      </div>

                      {/* Message Content */}
                      <div>
                        {msg.sender === 'bot' && (
                          <span className="text-xs text-indigo-600 font-medium mb-1 block flex items-center gap-1">
                            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            Teacher's Answer
                          </span>
                        )}
                        <div
                          className={`px-4 py-3 rounded-2xl text-base leading-relaxed ${
                            msg.sender === 'user'
                              ? 'bg-slate-200 text-slate-800 rounded-br-sm'
                              : 'bg-blue-50 text-slate-800 border border-blue-200 rounded-bl-sm shadow-sm'
                          }`}
                        >
                          <p className="whitespace-pre-wrap">{msg.text}</p>
                        </div>
                        
                        {/* Follow-up Question */}
                        {msg.sender === 'bot' && msg.followUpQuestion && (
                          <button
                            onClick={() => handleSend(msg.followUpQuestion!)}
                            className="mt-2 px-4 py-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 rounded-lg text-sm border border-indigo-200 transition-colors flex items-center gap-2"
                          >
                            <span className="text-lg">💡</span>
                            <span>{msg.followUpQuestion}</span>
                          </button>
                        )}
                        
                        <span className="text-xs text-slate-400 mt-1 block">{formatTime(msg.timestamp)}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {/* Typing Indicator */}
              {loading && (
                <div className="flex justify-start">
                  <div className="flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center">
                      <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                      </svg>
                    </div>
                    <div className="bg-white border border-slate-200 px-4 py-3 rounded-2xl rounded-bl-sm shadow-sm">
                      <div className="flex items-center gap-1.5">
                        <span className="w-2 h-2 bg-indigo-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></span>
                        <span className="w-2 h-2 bg-indigo-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></span>
                        <span className="w-2 h-2 bg-indigo-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></span>
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
              className="flex-1 bg-slate-100 border-0 rounded-xl px-4 py-3 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white text-base text-slate-800 placeholder-slate-400 resize-none min-h-[48px] max-h-[120px] transition-all"
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
              placeholder="What is photosynthesis?"
              disabled={loading}
              rows={1}
            />
            <button
              className={`h-12 px-6 rounded-xl font-medium flex items-center justify-center gap-2 transition-all ${
                input.trim() && !loading
                  ? 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-lg shadow-indigo-600/25'
                  : 'bg-slate-200 text-slate-400 cursor-not-allowed'
              }`}
              onClick={() => handleSend()}
              disabled={loading || !input.trim()}
            >
              {loading ? (
                <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
              ) : (
                <>
                  <span className="hidden sm:inline">Send</span>
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                  </svg>
                </>
              )}
            </button>
          </div>
          <p className="text-xs text-slate-400 text-center mt-3">
            Answers are generated from uploaded syllabus materials only • Press Enter to send
          </p>
        </div>
      </div>
    </div>
  );
};

export default ChatBot;
