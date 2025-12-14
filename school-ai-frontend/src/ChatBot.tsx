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

const quickQuestions = [
  { text: "What is Ohm's Law?", icon: "⚡" },
  { text: "Derive quadratic formula", icon: "📐" },
  { text: "Explain photosynthesis", icon: "🌿" },
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
        text: "Something went wrong. Please try again.",
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

  const hasMessages = messages.length > 0;

  return (
    <div className="study-container">
      {/* Context Bar - Sticky, Compact */}
      <div className="context-bar">
        <div className="context-bar-content">
          <span className="context-item">📚 Class 10</span>
          <span className="context-divider">•</span>
          <span className="context-item">Physics</span>
          <span className="context-divider">•</span>
          <span className="context-item">Current Electricity</span>
        </div>
        <div className="context-actions">
          <button
            onClick={handleNewSession}
            className="context-btn"
            title="New conversation"
          >
            ✨ New
          </button>
          <button
            onClick={() => setShowSessions(!showSessions)}
            className={`context-btn ${showSessions ? 'active' : ''}`}
            title="History"
          >
            📋 History
          </button>
        </div>
      </div>

      {/* Sessions Panel - Slide down */}
      {showSessions && (
        <div className="sessions-panel">
          <SessionsList 
            onSelectSession={(sid: string) => {
              setSessionId(sid);
              loadChatHistory(sid);
              setShowSessions(false);
            }}
          />
        </div>
      )}

      {/* Main Chat Card */}
      <div className="chat-card">
        {/* Messages Area */}
        <div className="chat-messages">
          
          {/* Empty State */}
          {!hasMessages && !loading && (
            <div className="empty-state">
              <div className="empty-icon">
                <span>💬</span>
              </div>
              <h2 className="empty-title">Ask a question from your syllabus</h2>
              <p className="empty-subtitle">I'll help you understand any topic</p>
              
              {/* Quick Question Chips */}
              <div className="quick-chips">
                {quickQuestions.map((q, i) => (
                  <button
                    key={i}
                    onClick={() => handleSend(q.text)}
                    className="quick-chip"
                  >
                    <span>{q.icon}</span>
                    <span>{q.text}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Chat Messages */}
          {hasMessages && (
            <div className="messages-list">
              {messages.map((msg, idx) => (
                <div
                  key={idx}
                  className={`message-row ${msg.sender === 'user' ? 'user' : 'bot'}`}
                >
                  <div className="message-bubble-wrapper">
                    {msg.sender === 'bot' && (
                      <div className="bot-avatar">
                        <span>📘</span>
                      </div>
                    )}
                    
                    <div className={`message-bubble ${msg.sender}`}>
                      <p>{msg.text}</p>
                    </div>
                  </div>
                  
                  {/* Follow-up Question */}
                  {msg.sender === 'bot' && msg.followUpQuestion && (
                    <button
                      onClick={() => handleSend(msg.followUpQuestion!)}
                      className="followup-btn"
                    >
                      <span>💡</span>
                      <span>{msg.followUpQuestion}</span>
                    </button>
                  )}
                </div>
              ))}

              {/* Typing Indicator */}
              {loading && (
                <div className="message-row bot">
                  <div className="message-bubble-wrapper">
                    <div className="bot-avatar">
                      <span>📘</span>
                    </div>
                    <div className="message-bubble bot typing">
                      <div className="typing-dots">
                        <span></span>
                        <span></span>
                        <span></span>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>
          )}
        </div>

        {/* Input Area - Sticky Bottom */}
        <div className="chat-input-area">
          <div className="input-wrapper">
            <textarea
              ref={inputRef}
              className="chat-input"
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
                target.style.height = Math.min(target.scrollHeight, 100) + 'px';
              }}
              placeholder="Type your question here..."
              disabled={loading}
              rows={1}
            />
            <button
              className={`send-btn ${input.trim() && !loading ? 'active' : ''}`}
              onClick={() => handleSend()}
              disabled={loading || !input.trim()}
            >
              {loading ? (
                <div className="send-spinner" />
              ) : (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M22 2L11 13" />
                  <path d="M22 2L15 22L11 13L2 9L22 2Z" />
                </svg>
              )}
            </button>
          </div>
          <p className="input-hint">Press Enter to send</p>
        </div>
      </div>

      {/* Footer - Single Line */}
      <div className="study-footer">
        <span>🔒</span>
        <span>Answers are generated from uploaded syllabus materials</span>
      </div>
    </div>
  );
};

export default ChatBot;
