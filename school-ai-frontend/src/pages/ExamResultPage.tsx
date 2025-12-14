import React, { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Bar } from "react-chartjs-2";
import {
  Chart,
  CategoryScale,
  LinearScale,
  BarElement,
  Tooltip,
  Legend,
} from "chart.js";
import type { ChartOptions } from "chart.js";
import { useToast } from "../hooks/useToast";

// Register Chart.js components
Chart.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend);

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface DifficultyStats {
  totalQuestions: number;
  correctAnswers: number;
  accuracy: number;
}

interface WeakChapter {
  chapter: string;
  subject: string;
  accuracy: number;
  questionsAttempted: number;
}

interface ExamTemplate {
  id: number;
  name: string;
  subject: string;
  chapter: string | null;
  totalQuestions: number;
  durationMinutes: number;
  adaptiveEnabled: boolean;
}

interface ResultSummary {
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
  weakChapters?: WeakChapter[];
}

type ExamSummaryResponse = ResultSummary;

// ==========================================
// API FUNCTIONS
// ==========================================

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080";

async function getExamSummary(attemptId: number): Promise<ExamSummaryResponse> {
  // Try plural route first (local), fallback to singular (production)
  const routes = [
    `${API_URL}/api/exams/${attemptId}/summary`,
    `${API_URL}/api/exam/${attemptId}/summary`
  ];
  
  for (const route of routes) {
    try {
      const response = await fetch(route, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
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
// MAIN COMPONENT
// ==========================================

interface ExamResultPageProps {
  attemptId: number;
  onRetakeExam?: (templateId: number) => void;
  toast: ReturnType<typeof useToast>;
}

const ExamResultPage: React.FC<ExamResultPageProps> = ({
  attemptId,
  onRetakeExam,
  toast,
}) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<ResultSummary | null>(null);

  // Fetch exam summary on mount
  useEffect(() => {
    const fetchSummary = async () => {
      try {
        setLoading(true);
        const data = await getExamSummary(attemptId);
        setSummary(data);
        setError(null);
      } catch (err) {
        const errorMsg =
          err instanceof Error ? err.message : "Failed to load exam results";
        setError(errorMsg);
        toast.showToast(errorMsg, "error");
      } finally {
        setLoading(false);
      }
    };

    fetchSummary();
  }, [attemptId, toast]);

  // Retry handler
  const handleRetry = () => {
    setError(null);
    setLoading(true);
    window.location.reload();
  };

  // Retake exam handler
  const handleRetakeExam = () => {
    if (summary?.template.id) {
      onRetakeExam?.(summary.template.id);
    }
  };

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
      <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 flex items-center justify-center px-6">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="text-5xl mb-4">😅</div>
          <h2 className="text-2xl font-bold text-slate-800 mb-2">
            Oops! Something went wrong
          </h2>
          <p className="text-slate-600 mb-6">{error || "No results available"}</p>
          <button
            onClick={handleRetry}
            className="px-6 py-3 bg-gradient-to-r from-cyan-500 to-teal-500 text-white rounded-xl font-medium hover:shadow-lg transition-all duration-200"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  const passed = summary.scorePercent >= 60;

  // Prepare chart data
  const chartData = {
    labels: ["Correct", "Wrong"],
    datasets: [
      {
        label: "Answers",
        data: [summary.correctCount, summary.wrongCount],
        backgroundColor: ["rgba(34, 197, 94, 0.8)", "rgba(239, 68, 68, 0.8)"],
        borderColor: ["rgba(34, 197, 94, 1)", "rgba(239, 68, 68, 1)"],
        borderWidth: 2,
      },
    ],
  };

  const chartOptions: ChartOptions<"bar"> = {
    responsive: true,
    maintainAspectRatio: true,
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        callbacks: {
          label: function (context) {
            return `${context.label}: ${context.parsed.y} questions`;
          },
        },
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          stepSize: 1,
        },
      },
    },
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 py-8">
      <div className="max-w-4xl mx-auto px-6">
        {/* Hero Card - Score Display */}
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
                ? "bg-gradient-to-br from-emerald-400 to-green-500"
                : "bg-gradient-to-br from-amber-400 to-orange-500"
            }`}
          >
            <span className="text-5xl">{passed ? '🎉' : '💪'}</span>
          </motion.div>

          <h1 className="text-3xl lg:text-4xl font-bold text-slate-800 mb-2">
            {passed ? "Congratulations! 🎉" : "Keep Practicing! 💪"}
          </h1>
          <p className="text-lg text-slate-600 mb-6">
            {passed ? "You passed the exam!" : "Better luck next time!"}
          </p>

          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2, type: "spring", stiffness: 150 }}
            className="inline-block mb-4"
          >
            <div
              className={`text-7xl font-bold ${
                passed ? "text-emerald-600" : "text-amber-600"
              }`}
            >
              {summary.scorePercent.toFixed(1)}%
            </div>
          </motion.div>

          <p className="text-xl text-slate-700 font-semibold">
            Correct {summary.correctCount} / Wrong {summary.wrongCount}
          </p>
        </motion.div>

        {/* Chart Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="bg-white rounded-xl shadow-lg p-6 mb-6"
        >
          <h2 className="text-2xl font-bold text-gray-800 mb-6 text-center">
            Performance Overview
          </h2>
          <div className="max-w-md mx-auto">
            <Bar data={chartData} options={chartOptions} />
          </div>
        </motion.div>

        {/* Exam Details */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="bg-white rounded-xl shadow-lg p-6 mb-6"
        >
          <h2 className="text-xl font-bold text-gray-800 mb-4">Exam Details</h2>
          <div className="space-y-3">
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Exam Name</span>
              <span className="font-semibold text-gray-800">
                {summary.template.name}
              </span>
            </div>
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-slate-600">Subject</span>
              <span className="font-semibold text-teal-600">
                {summary.template.subject}
              </span>
            </div>
            {summary.template.chapter && (
              <div className="flex justify-between items-center pb-3 border-b">
                <span className="text-gray-600">Chapter</span>
                <span className="font-semibold text-teal-600">
                  {summary.template.chapter}
                </span>
              </div>
            )}
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Total Questions</span>
              <span className="font-semibold text-gray-800">
                {summary.totalQuestions}
              </span>
            </div>
            <div className="flex justify-between items-center pb-3 border-b">
              <span className="text-gray-600">Status</span>
              <span
                className={`px-3 py-1 rounded-full text-sm font-semibold ${
                  summary.status === "Completed"
                    ? "bg-green-100 text-green-700"
                    : "bg-yellow-100 text-yellow-700"
                }`}
              >
                {summary.status}
              </span>
            </div>
          </div>
        </motion.div>

        {/* Per-Difficulty Stats */}
        {Object.keys(summary.perDifficultyStats).length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
            className="bg-white rounded-xl shadow-lg p-6 mb-6"
          >
            <h2 className="text-xl font-bold text-gray-800 mb-6">
              Difficulty Breakdown
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {Object.entries(summary.perDifficultyStats).map(
                ([difficulty, stats]) => {
                  const colors = {
                    Easy: {
                      bg: "bg-green-50",
                      border: "border-green-200",
                      text: "text-green-700",
                      accent: "text-green-600",
                      progress: "bg-green-500",
                    },
                    Medium: {
                      bg: "bg-yellow-50",
                      border: "border-yellow-200",
                      text: "text-yellow-700",
                      accent: "text-yellow-600",
                      progress: "bg-yellow-500",
                    },
                    Hard: {
                      bg: "bg-red-50",
                      border: "border-red-200",
                      text: "text-red-700",
                      accent: "text-red-600",
                      progress: "bg-red-500",
                    },
                  };

                  const colorScheme =
                    colors[difficulty as keyof typeof colors];

                  return (
                    <div
                      key={difficulty}
                      className={`${colorScheme.bg} border-2 ${colorScheme.border} rounded-xl p-6`}
                    >
                      <h3
                        className={`text-lg font-bold ${colorScheme.text} mb-4`}
                      >
                        {difficulty}
                      </h3>
                      <div className="space-y-3">
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Questions</span>
                          <span className={`font-bold ${colorScheme.accent}`}>
                            {stats.totalQuestions}
                          </span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Correct</span>
                          <span className={`font-bold ${colorScheme.accent}`}>
                            {stats.correctAnswers}
                          </span>
                        </div>
                        <div className="mt-4">
                          <div className="flex justify-between text-sm mb-2">
                            <span className="text-gray-600">Accuracy</span>
                            <span className={`font-bold ${colorScheme.accent}`}>
                              {stats.accuracy.toFixed(1)}%
                            </span>
                          </div>
                          <div className="w-full bg-gray-200 rounded-full h-3">
                            <motion.div
                              initial={{ width: 0 }}
                              animate={{ width: `${stats.accuracy}%` }}
                              transition={{ duration: 1, ease: "easeOut" }}
                              className={`${colorScheme.progress} h-3 rounded-full`}
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                }
              )}
            </div>
          </motion.div>
        )}

        {/* Weak Chapters Section */}
        {summary.weakChapters && summary.weakChapters.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4 }}
            className="bg-white rounded-xl shadow-lg p-6 mb-6"
          >
            <h2 className="text-xl font-bold text-gray-800 mb-4">
              Areas to Improve
            </h2>
            <p className="text-gray-600 mb-4">
              Focus on these chapters to strengthen your knowledge
            </p>
            <div className="space-y-3">
              {summary.weakChapters
                .sort((a, b) => a.accuracy - b.accuracy)
                .map((chapter, index) => (
                  <motion.div
                    key={index}
                    whileHover={{ scale: 1.02 }}
                    className="bg-orange-50 border-l-4 border-orange-500 rounded-lg p-4 cursor-pointer hover:bg-orange-100 transition-colors"
                  >
                    <div className="flex justify-between items-center">
                      <div>
                        <h3 className="font-semibold text-gray-800">
                          {chapter.chapter}
                        </h3>
                        <p className="text-sm text-gray-600">
                          {chapter.subject} • {chapter.questionsAttempted}{" "}
                          questions
                        </p>
                      </div>
                      <div className="text-right">
                        <div className="text-2xl font-bold text-orange-600">
                          {chapter.accuracy.toFixed(0)}%
                        </div>
                        <div className="text-xs text-gray-500">accuracy</div>
                      </div>
                    </div>
                  </motion.div>
                ))}
            </div>
          </motion.div>
        )}

        {/* Sticky Bottom Button */}
        <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-slate-200 shadow-2xl">
          <div className="max-w-4xl mx-auto px-6 py-4">
            <button
              onClick={handleRetakeExam}
              className="w-full px-8 py-4 bg-gradient-to-r from-cyan-500 to-teal-500 text-white rounded-xl font-bold text-lg hover:shadow-lg transition-all duration-200 hover:scale-105"
            >
              <span className="flex items-center justify-center gap-2">
                <span>🔄</span>
                Retake Exam
              </span>
            </button>
          </div>
        </div>

        {/* Bottom padding to prevent content being hidden by sticky button */}
        <div className="h-24"></div>
      </div>
    </div>
  );
};

export default ExamResultPage;
