import React, { useState, useEffect, useRef } from "react";
import { sendChat, API_URL } from "./api";
import { motion } from "framer-motion";

interface Message {
  sender: "user" | "bot";
  text: string;
  timestamp: Date;
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

const ChatBot: React.FC<{ token?: string }> = ({ token }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [showSuggestions, setShowSuggestions] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const userId = 1;
  const schoolId = 1;
  const language = "en";

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    // Welcome message
    const welcomeMessage: Message = {
      sender: "bot",
      text: "Welcome To The Smart Neurozic AI Chat Bot...!!",
      timestamp: new Date()
    };
    setMessages([welcomeMessage]);
  }, []);

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
      const res = await sendChat({ userId, schoolId, message: messageText, language, token });
      const botMessage: Message = {
        sender: "bot",
        text: res.reply || "I apologize, but I couldn't generate a response. Please try again.",
        timestamp: new Date()
      };
      // Clear any previous server error once we receive a successful response
      setServerError(null);
      setMessages((msgs) => [...msgs, botMessage]);
    } catch (err) {
      // Friendly message plus set a visible server error banner so user can retry
      const errorMessage: Message = {
        sender: "bot",
        text: "😔 Oops! I couldn't reach the server. Please check your connection or try again.",
        timestamp: new Date()
      };
      setMessages((msgs) => [...msgs, errorMessage]);
      setServerError(`Cannot connect to the backend at ${API_URL}.`);
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async () => {
    setLoading(true);
    try {
      // Try a simple GET to the API root or health endpoint
      const url = API_URL.endsWith('/') ? API_URL : API_URL + '/';
      const res = await fetch(url, { method: 'GET' });
      if (res.ok) {
        setServerError(null);
        // Optionally add a small confirmation message from the bot
        const okMsg: Message = { sender: 'bot', text: '✅ Server is reachable again. How can I help?', timestamp: new Date() };
        setMessages((m) => [...m, okMsg]);
      } else {
        setServerError(`Server responded with status ${res.status}. Please check the backend.`);
      }
    } catch (e) {
      setServerError(`Still unable to reach the backend at ${API_URL}.`);
    } finally {
      setLoading(false);
    }
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
              Always here to help
            </p>
          </div>
        </div>
        <div className="text-right">
          <div className="text-sm font-semibold">{messages.length - 1} messages</div>
          <div className="text-xs opacity-75">Powered by Neurozic AI</div>
        </div>
      </div>

      {/* Server error banner */}
      {serverError && (
        <div className="bg-red-50 border-l-4 border-red-400 text-red-700 px-4 py-3 flex items-center justify-between" role="alert">
          <div className="flex items-center gap-3">
            <svg className="w-5 h-5 text-red-500" fill="currentColor" viewBox="0 0 20 20"><path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.681-1.36 3.446 0l5.518 9.807c.75 1.333-.213 2.994-1.723 2.994H4.462c-1.51 0-2.473-1.661-1.723-2.994L8.257 3.1zM11 13a1 1 0 10-2 0 1 1 0 002 0zm-1-8a1 1 0 01.894.553l.5 1a1 1 0 11-1.788.894L9.106 6.45A1 1 0 0110 5z" clipRule="evenodd"/></svg>
            <div>
              <p className="font-medium">Server unreachable</p>
              <p className="text-sm">{serverError} — please check the backend or your connection.</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={handleRetry} className="bg-red-500 text-white px-3 py-1 rounded-md hover:bg-red-600 disabled:opacity-50" disabled={loading}>
              Retry
            </button>
            <button onClick={() => setServerError(null)} className="text-red-600 px-2 py-1 rounded-md hover:bg-red-100">
              Dismiss
            </button>
          </div>
        </div>
      )}

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
              <div className={`flex flex-col ${msg.sender === "user" ? "items-end" : "items-start"}`}>
                <div
                  className={`px-5 py-3 rounded-2xl text-base max-w-[75vw] sm:max-w-xl break-words shadow-md transition-all duration-300 hover:shadow-lg ${
                    msg.sender === "user"
                      ? "bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-br-sm"
                      : "bg-white text-gray-800 border border-gray-200 rounded-bl-sm"
                  }`}
                >
                  {msg.text}
                </div>
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
