import React, { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { useToast } from "../hooks/useToast";

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface ExamTemplate {
  id: number;
  name: string;
  subject: string;
  chapter: string | null;
  totalQuestions: number;
  durationMinutes: number;
  adaptiveEnabled: boolean;
  createdAt: string;
}

interface DifficultyStats {
  totalQuestions: number;
  correctAnswers: number;
  accuracy: number;
}

interface ExamSummary {
  attemptId: number;
  studentId: string;
  template: ExamTemplate;
  scorePercent: number;
  correctCount: number;
  wrongCount: number;
  totalQuestions: number;
  startedAt: string;
  completedAt: string | null;
  status: string;
  perDifficultyStats: {
    Easy?: DifficultyStats;
    Medium?: DifficultyStats;
    Hard?: DifficultyStats;
  };
}

// ==========================================
// API FUNCTIONS
// ==========================================

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

async function getExamSummary(attemptId: number): Promise<ExamSummary> {
  // Try plural route first (local), fallback to singular (production)
  const routes = [
    `${API_URL}/api/exams/${attemptId}/summary`,
    `${API_URL}/api/exam/${attemptId}/summary`
  ];
  
  for (const route of routes) {
    try {
      const response = await fetch(route, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
      });
      
      if (response.ok) {
        return response.json();
      }
    } catch (error) {
      // Try next route
    }
  }
  
  throw new Error('Failed to fetch exam summary');
}

// ==========================================
// DIFFICULTY STATS COMPONENT
// ==========================================

interface DifficultyCardProps {
  difficulty: string;
  stats: DifficultyStats;
  color: string;
}

const DifficultyCard: React.FC<DifficultyCardProps> = ({ difficulty, stats, color }) => {
  const colorClasses = {
    green: {
      bg: 'bg-green-50',
      border: 'border-green-200',
      text: 'text-green-700',
      accent: 'text-green-600',
      progress: 'bg-green-500',
    },
    yellow: {
      bg: 'bg-yellow-50',
      border: 'border-yellow-200',
      text: 'text-yellow-700',
      accent: 'text-yellow-600',
      progress: 'bg-yellow-500',
    },
    red: {
      bg: 'bg-red-50',
      border: 'border-red-200',
      text: 'text-red-700',
      accent: 'text-red-600',
      progress: 'bg-red-500',
    },
  };

  const colors = colorClasses[color as keyof typeof colorClasses];

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className={`${colors.bg} border-2 ${colors.border} rounded-xl p-6`}
    >
      <h3 className={`text-lg font-bold ${colors.text} mb-4`}>{difficulty}</h3>
      <div className="space-y-3">
        <div className="flex justify-between text-sm">
          <span className="text-gray-600">Questions</span>
          <span className={`font-bold ${colors.accent}`}>{stats.totalQuestions}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-600">Correct</span>
          <span className={`font-bold ${colors.accent}`}>{stats.correctAnswers}</span>
        </div>
        <div className="mt-4">
          <div className="flex justify-between text-sm mb-2">
            <span className="text-gray-600">Accuracy</span>
            <span className={`font-bold ${colors.accent}`}>{stats.accuracy.toFixed(1)}%</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-3">
            <motion.div
              initial={{ width: 0 }}
              animate={{ width: `${stats.accuracy}%` }}
              transition={{ duration: 1, ease: "easeOut" }}
              className={`${colors.progress} h-3 rounded-full`}
            />
          </div>
        </div>
      </div>
    </motion.div>
  );
};

// ==========================================
// MAIN EXAM RESULT COMPONENT
// ==========================================

interface ExamResultProps {
  attemptId: number;
  onBackToHome?: () => void;
  toast: ReturnType<typeof useToast>;
}

const ExamResult: React.FC<ExamResultProps> = ({ attemptId, onBackToHome, toast }) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<ExamSummary | null>(null);

  useEffect(() => {
    const fetchSummary = async () => {
      try {
        setLoading(true);
        const data = await getExamSummary(attemptId);
        setSummary(data);
        setError(null);
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : 'Failed to load exam results';
        setError(errorMsg);
        toast.showToast(errorMsg, 'error');
      } finally {
        setLoading(false);
      }
    };

    fetchSummary();
  }, [attemptId, toast]);

  // Loading state
  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-lg text-gray-600 font-medium">Loading results...</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error || !summary) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-red-500" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">Error Loading Results</h2>
          <p className="text-gray-600 mb-6">{error || 'No results available'}</p>
          <button
            onClick={onBackToHome}
            className="px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg font-medium hover:shadow-lg transition-all duration-200"
          >
            Back to Home
          </button>
        </div>
      </div>
    );
  }

  const passed = summary.scorePercent >= 60;
  const difficultyColors = {
    Easy: 'green',
    Medium: 'yellow',
    Hard: 'red',
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 py-8">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header Card */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="bg-white rounded-2xl shadow-2xl p-8 mb-6 text-center"
        >
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ type: "spring", stiffness: 200, damping: 15 }}
            className={`w-24 h-24 mx-auto mb-6 rounded-full flex items-center justify-center ${
              passed
                ? 'bg-gradient-to-br from-green-400 to-emerald-500'
                : 'bg-gradient-to-br from-orange-400 to-red-500'
            }`}
          >
            {passed ? (
              <svg className="w-12 h-12 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            ) : (
              <svg className="w-12 h-12 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            )}
          </motion.div>

          <h1 className="text-3xl lg:text-4xl font-bold text-gray-800 mb-2">
            {passed ? 'Congratulations! 🎉' : 'Keep Practicing! 💪'}
          </h1>
          <p className="text-lg text-gray-600 mb-6">
            {passed ? 'You passed the exam!' : 'Better luck next time!'}
          </p>

          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2, type: "spring", stiffness: 150 }}
            className="inline-block"
          >
            <div className={`text-7xl font-bold mb-2 ${
              passed ? 'text-green-600' : 'text-orange-600'
            }`}>
              {summary.scorePercent.toFixed(1)}%
            </div>
            <div className="text-sm text-gray-500 uppercase tracking-wide">Final Score</div>
          </motion.div>
        </motion.div>

        {/* Exam Details */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="bg-white rounded-xl shadow-lg p-6 mb-6"
        >
          <h2 className="text-xl font-bold text-gray-800 mb-4">Exam Details</h2>
          <div className="space-y-3">
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Exam Name</span>
              <span className="font-semibold text-gray-800">{summary.template.name}</span>
            </div>
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Subject</span>
              <span className="font-semibold text-indigo-600">{summary.template.subject}</span>
            </div>
            {summary.template.chapter && (
              <div className="flex justify-between items-center pb-3 border-b">
                <span className="text-gray-600">Chapter</span>
                <span className="font-semibold text-purple-600">{summary.template.chapter}</span>
              </div>
            )}
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Status</span>
              <span className={`px-3 py-1 rounded-full text-sm font-semibold ${
                summary.status === 'Completed' 
                  ? 'bg-green-100 text-green-700'
                  : 'bg-yellow-100 text-yellow-700'
              }`}>
                {summary.status}
              </span>
            </div>
            {summary.template.adaptiveEnabled && (
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Adaptive Mode</span>
                <span className="px-3 py-1 bg-yellow-100 text-yellow-700 rounded-full text-sm font-semibold">
                  ⚡ Enabled
                </span>
              </div>
            )}
          </div>
        </motion.div>

        {/* Performance Overview */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="bg-white rounded-xl shadow-lg p-6 mb-6"
        >
          <h2 className="text-xl font-bold text-gray-800 mb-6">Performance Overview</h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="bg-gradient-to-br from-indigo-50 to-purple-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-indigo-600 mb-2">
                {summary.totalQuestions}
              </div>
              <div className="text-sm text-gray-600 uppercase tracking-wide">Total Questions</div>
            </div>
            <div className="bg-gradient-to-br from-green-50 to-emerald-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-green-600 mb-2">
                {summary.correctCount}
              </div>
              <div className="text-sm text-gray-600 uppercase tracking-wide">Correct Answers</div>
            </div>
            <div className="bg-gradient-to-br from-red-50 to-pink-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-red-600 mb-2">
                {summary.wrongCount}
              </div>
              <div className="text-sm text-gray-600 uppercase tracking-wide">Wrong Answers</div>
            </div>
          </div>
        </motion.div>

        {/* Per-Difficulty Statistics */}
        {Object.keys(summary.perDifficultyStats).length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
            className="bg-white rounded-xl shadow-lg p-6 mb-6"
          >
            <h2 className="text-xl font-bold text-gray-800 mb-6">Difficulty Breakdown</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {Object.entries(summary.perDifficultyStats).map(([difficulty, stats]) => (
                <DifficultyCard
                  key={difficulty}
                  difficulty={difficulty}
                  stats={stats}
                  color={difficultyColors[difficulty as keyof typeof difficultyColors]}
                />
              ))}
            </div>
          </motion.div>
        )}

        {/* Action Buttons */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="flex flex-col sm:flex-row gap-4"
        >
          <button
            onClick={onBackToHome}
            className="flex-1 px-6 py-4 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-xl font-bold text-lg hover:shadow-xl transition-all duration-200 hover:scale-105"
          >
            <span className="flex items-center justify-center gap-2">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" />
              </svg>
              Back to Home
            </span>
          </button>
          <button
            onClick={() => window.location.reload()}
            className="flex-1 px-6 py-4 bg-white border-2 border-indigo-500 text-indigo-600 rounded-xl font-bold text-lg hover:bg-indigo-50 transition-all duration-200 hover:scale-105"
          >
            <span className="flex items-center justify-center gap-2">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clipRule="evenodd" />
              </svg>
              Retake Exam
            </span>
          </button>
        </motion.div>
      </div>
    </div>
  );
};

export default ExamResult;
