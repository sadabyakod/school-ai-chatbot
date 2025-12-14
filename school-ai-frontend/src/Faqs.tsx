import React, { useEffect, useState } from "react";
import { getFaqs, ApiException } from "./api";
import { useToast } from "./hooks/useToast";
import { motion, AnimatePresence } from "framer-motion";

interface FAQ {
  id: string | number;
  question: string;
  answer: string;
}

const FAQItem: React.FC<{
  faq: FAQ;
  index: number;
  isOpen: boolean;
  onToggle: () => void;
}> = ({ faq, index, isOpen, onToggle }) => (
  <motion.div
    className="overflow-hidden"
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.3, delay: index * 0.05 }}
  >
    <motion.button
      className={`w-full text-left p-5 rounded-2xl transition-all ${
        isOpen 
          ? 'bg-gradient-to-r from-cyan-500 to-teal-600 text-white shadow-lg shadow-cyan-500/25' 
          : 'bg-white hover:bg-slate-50 border border-slate-200 hover:border-cyan-300 hover:shadow-md'
      }`}
      onClick={onToggle}
      whileHover={{ scale: 1.01 }}
      whileTap={{ scale: 0.99 }}
    >
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <span className={`flex-shrink-0 w-8 h-8 rounded-xl flex items-center justify-center text-sm font-bold ${
            isOpen ? 'bg-white/20 text-white' : 'bg-cyan-100 text-cyan-600'
          }`}>
            {index + 1}
          </span>
          <span className={`font-semibold ${isOpen ? 'text-white' : 'text-slate-800'}`}>
            {faq.question}
          </span>
        </div>
        <motion.div
          animate={{ rotate: isOpen ? 180 : 0 }}
          transition={{ duration: 0.3 }}
          className={`flex-shrink-0 w-6 h-6 rounded-full flex items-center justify-center ${
            isOpen ? 'bg-white/20' : 'bg-slate-100'
          }`}
        >
          <span className={`text-sm ${isOpen ? 'text-white' : 'text-slate-500'}`}>{isOpen ? '▲' : '▼'}</span>
        </motion.div>
      </div>
    </motion.button>
    
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: "auto", opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          transition={{ duration: 0.3 }}
          className="overflow-hidden"
        >
          <div className="px-5 py-4 ml-12 mt-2 bg-gradient-to-r from-cyan-50 to-teal-50 rounded-xl border border-cyan-100">
            <div className="flex items-start gap-3">
              <span className="flex-shrink-0 mt-0.5 w-6 h-6 rounded-lg bg-gradient-to-br from-cyan-500 to-teal-500 flex items-center justify-center">
                <span className="text-sm text-white">✓</span>
              </span>
              <p className="text-slate-700 leading-relaxed">{faq.answer}</p>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  </motion.div>
);

const Faqs: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [faqs, setFaqs] = useState<FAQ[]>([]);
  const [loading, setLoading] = useState(false);
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  useEffect(() => {
    const fetchFaqs = async () => {
      setLoading(true);
      try {
        const data = await getFaqs(token);
        setFaqs(data as FAQ[]);
      } catch (err) {
        const error = err as ApiException;
        toast.error(error.message || "Couldn't load help topics right now");
        setFaqs([]);
      } finally {
        setLoading(false);
      }
    };
    fetchFaqs();
  }, [token, toast]);

  const handleToggle = (index: number) => {
    setOpenIndex(openIndex === index ? null : index);
  };

  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-6">
      {/* Header */}
      <motion.div 
        className="text-center mb-8"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
      >
        <div className="page-header-icon mx-auto">
          <span className="text-2xl">💡</span>
        </div>
        <h2 className="page-header-title text-gradient">Help & Support</h2>
        <p className="page-header-subtitle">Common questions about Smart Study</p>
      </motion.div>

      {loading ? (
        <motion.div 
          className="card p-12 text-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          <div className="relative inline-flex">
            <div className="w-16 h-16 border-4 border-slate-200 rounded-full" />
            <div className="w-16 h-16 border-4 border-cyan-500 border-t-transparent rounded-full absolute top-0 left-0 animate-spin" />
          </div>
          <p className="text-slate-500 mt-4 font-medium">Loading help topics...</p>
        </motion.div>
      ) : faqs.length === 0 ? (
        <motion.div 
          className="empty-state"
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
        >
          <div className="empty-state-icon">📚</div>
          <h3 className="empty-state-title">Help Topics Coming Soon</h3>
          <p className="empty-state-text">
            We're working on adding helpful guides and answers. 
            In the meantime, try asking our Study Assistant!
          </p>
        </motion.div>
      ) : (
        <motion.div 
          className="space-y-3"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.4 }}
        >
          {/* FAQ Count Badge */}
          <div className="flex justify-center mb-6">
            <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white border border-slate-200 text-slate-600 text-sm font-medium shadow-sm">
              <span>📖</span>
              {faqs.length} Help Topics
            </span>
          </div>

          {/* FAQ List */}
          {faqs.map((faq, index) => (
            <FAQItem
              key={faq.id}
              faq={faq}
              index={index}
              isOpen={openIndex === index}
              onToggle={() => handleToggle(index)}
            />
          ))}

          {/* Help Section */}
          <motion.div
            className="info-box mt-8 text-center"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.5 }}
          >
            <p className="text-cyan-800 font-medium mb-2">Still have questions?</p>
            <p className="text-cyan-700 text-sm">
              Ask our Study Assistant in the <span className="font-semibold">Study</span> tab - it knows your entire syllabus! 📚
            </p>
          </motion.div>
        </motion.div>
      )}
    </div>
  );
};

export default Faqs;
