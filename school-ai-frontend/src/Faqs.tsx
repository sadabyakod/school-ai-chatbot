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
      className={`w-full text-left p-5 rounded-2xl transition-all duration-300 ${
        isOpen 
          ? 'bg-gradient-to-r from-violet-500 via-purple-500 to-fuchsia-500 text-white shadow-lg shadow-purple-500/25' 
          : 'bg-white hover:bg-gray-50 border border-gray-200 hover:border-purple-300 hover:shadow-md'
      }`}
      onClick={onToggle}
      whileHover={{ scale: 1.01 }}
      whileTap={{ scale: 0.99 }}
    >
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <span className={`flex-shrink-0 w-8 h-8 rounded-xl flex items-center justify-center text-sm font-bold ${
            isOpen ? 'bg-white/20 text-white' : 'bg-purple-100 text-purple-600'
          }`}>
            {index + 1}
          </span>
          <span className={`font-semibold ${isOpen ? 'text-white' : 'text-gray-800'}`}>
            {faq.question}
          </span>
        </div>
        <motion.div
          animate={{ rotate: isOpen ? 180 : 0 }}
          transition={{ duration: 0.3 }}
          className={`flex-shrink-0 w-6 h-6 rounded-full flex items-center justify-center ${
            isOpen ? 'bg-white/20' : 'bg-gray-100'
          }`}
        >
          <svg 
            className={`w-4 h-4 ${isOpen ? 'text-white' : 'text-gray-500'}`} 
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
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
          <div className="px-5 py-4 ml-12 mt-2 bg-gradient-to-r from-purple-50 to-fuchsia-50 rounded-xl border border-purple-100">
            <div className="flex items-start gap-3">
              <span className="flex-shrink-0 mt-0.5 w-6 h-6 rounded-lg bg-gradient-to-br from-violet-500 to-purple-500 flex items-center justify-center">
                <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </span>
              <p className="text-gray-700 leading-relaxed">{faq.answer}</p>
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
        toast.error(error.message || "Failed to load FAQs");
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
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-6 sm:py-8">
      {/* Header */}
      <motion.div 
        className="text-center mb-8"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <motion.div 
          className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-500 via-purple-500 to-fuchsia-500 mb-4 shadow-lg shadow-purple-500/25"
          whileHover={{ scale: 1.05, rotate: 5 }}
        >
          <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </motion.div>
        <h2 className="text-2xl sm:text-3xl font-bold text-gradient mb-2">Frequently Asked Questions</h2>
        <p className="text-gray-500">Find answers to common questions about our platform</p>
      </motion.div>

      {loading ? (
        <motion.div 
          className="glass rounded-3xl p-12 text-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          <div className="relative inline-flex">
            <div className="w-16 h-16 border-4 border-purple-200 rounded-full" />
            <div className="w-16 h-16 border-4 border-purple-500 border-t-transparent rounded-full absolute top-0 left-0 animate-spin" />
          </div>
          <p className="text-gray-500 mt-4 font-medium">Loading FAQs...</p>
        </motion.div>
      ) : faqs.length === 0 ? (
        <motion.div 
          className="glass rounded-3xl p-12 text-center"
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
        >
          <div className="w-20 h-20 mx-auto mb-4 rounded-2xl bg-gray-100 flex items-center justify-center">
            <svg className="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">No FAQs Available</h3>
          <p className="text-gray-500">FAQs will appear here once they are added.</p>
        </motion.div>
      ) : (
        <motion.div 
          className="space-y-3"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.5 }}
        >
          {/* FAQ Count Badge */}
          <div className="flex justify-center mb-6">
            <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white border border-gray-200 text-gray-600 text-sm font-medium shadow-sm">
              <svg className="w-4 h-4 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
              </svg>
              {faqs.length} Questions
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
            className="mt-8 p-6 glass rounded-2xl text-center"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, delay: 0.5 }}
          >
            <p className="text-gray-600 mb-3">Can't find what you're looking for?</p>
            <p className="text-gray-500 text-sm">
              Ask our AI assistant in the <span className="font-semibold text-purple-600">Chat</span> tab for personalized help!
            </p>
          </motion.div>
        </motion.div>
      )}
    </div>
  );
};

export default Faqs;
