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
      <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-cyan-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-lg text-slate-600 font-medium">Loading your results...</p>
          <p className="text-sm text-slate-500 mt-2">🔒 Exam Safe • Syllabus Only</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error || !summary) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="text-5xl mb-4">😅</div>
          <h2 className="text-2xl font-bold text-slate-800 mb-2">Oops! Something went wrong</h2>
          <p className="text-slate-600 mb-6">{error || 'No results available'}</p>
          <button
            onClick={onBackToHome}
            className="px-6 py-3 bg-gradient-to-r from-cyan-500 to-teal-500 text-white rounded-xl font-medium hover:shadow-lg transition-all duration-200"
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
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 py-8">
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
                ? 'bg-gradient-to-br from-emerald-400 to-green-500'
                : 'bg-gradient-to-br from-amber-400 to-orange-500'
            }`}
          >
            <span className="text-5xl">{passed ? '🎉' : '💪'}</span>
          </motion.div>

          <h1 className="text-3xl lg:text-4xl font-bold text-slate-800 mb-2">
            {passed ? 'Congratulations! 🎉' : 'Keep Practicing! 💪'}
          </h1>
          <p className="text-lg text-slate-600 mb-6">
            {passed ? 'You passed the exam!' : 'Better luck next time!'}
          </p>

          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2, type: "spring", stiffness: 150 }}
            className="inline-block"
          >
            <div className={`text-7xl font-bold mb-2 ${
              passed ? 'text-emerald-600' : 'text-amber-600'
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
              <span className="text-slate-600">Subject</span>
              <span className="font-semibold text-teal-600">{summary.template.subject}</span>
            </div>
            {summary.template.chapter && (
              <div className="flex justify-between items-center pb-3 border-b">
                <span className="text-gray-600">Chapter</span>
                <span className="font-semibold text-teal-600">{summary.template.chapter}</span>
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
            <div className="bg-gradient-to-br from-cyan-50 to-teal-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-teal-600 mb-2">
                {summary.totalQuestions}
              </div>
              <div className="text-sm text-slate-600 uppercase tracking-wide">Total Questions</div>
            </div>
            <div className="bg-gradient-to-br from-emerald-50 to-green-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-emerald-600 mb-2">
                {summary.correctCount}
              </div>
              <div className="text-sm text-slate-600 uppercase tracking-wide">Correct Answers</div>
            </div>
            <div className="bg-gradient-to-br from-rose-50 to-red-50 rounded-xl p-6 text-center">
              <div className="text-4xl font-bold text-rose-600 mb-2">
                {summary.wrongCount}
              </div>
              <div className="text-sm text-slate-600 uppercase tracking-wide">Wrong Answers</div>
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
            className="flex-1 px-6 py-4 bg-gradient-to-r from-cyan-500 to-teal-500 text-white rounded-xl font-bold text-lg hover:shadow-xl transition-all duration-200 hover:scale-105"
          >
            <span className="flex items-center justify-center gap-2">
              <span>🏠</span>
              Back to Home
            </span>
          </button>
          <button
            onClick={() => window.location.reload()}
            className="flex-1 px-6 py-4 bg-white border-2 border-cyan-500 text-cyan-600 rounded-xl font-bold text-lg hover:bg-cyan-50 transition-all duration-200 hover:scale-105"
          >
            <span className="flex items-center justify-center gap-2">
              <span>🔄</span>
              Retake Exam
            </span>
          </button>
        </motion.div>
      </div>
    </div>
  );
};

export default ExamResult;
