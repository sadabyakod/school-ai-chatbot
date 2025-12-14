import React, { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { useToast } from "../hooks/useToast";
import ExamPage from "./ExamPage";
import ExamResult from "./ExamResult";
import WrittenExamUpload from "./WrittenExamUpload";
import ProcessingStatus from "./ProcessingStatus";
import WrittenExamResultView from "./WrittenExamResult";
import type { WrittenExamResult } from "../api";

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

interface ExamHistory {
  attemptId: number;
  examName: string;
  subject: string;
  chapter: string | null;
  scorePercent: number;
  correctCount: number;
  wrongCount: number;
  status: string;
  startedAt: string;
  completedAt: string | null;
}

// ==========================================
// API FUNCTIONS
// ==========================================

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

async function getExamHistory(studentId: string): Promise<ExamHistory[]> {
  // Try plural route first (local), fallback to singular (production)
  const routes = [`${API_URL}/api/exams/history`, `${API_URL}/api/exam/history`];
  
  for (const route of routes) {
    try {
      const response = await fetch(`${route}?studentId=${studentId}`, {
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
  
  throw new Error('Failed to fetch exam history from both endpoints');
}

// Mock function to get available templates (you should create this endpoint in backend)
async function getExamTemplates(): Promise<ExamTemplate[]> {
  // For now, return mock data
  // TODO: Create GET /api/exams/templates endpoint in backend
  return [
    {
      id: 1,
      name: "Mathematics Chapter 1 Test",
      subject: "Mathematics",
      chapter: "Algebra",
      totalQuestions: 10,
      durationMinutes: 30,
      adaptiveEnabled: true,
      createdAt: new Date().toISOString()
    }
  ];
}

// ==========================================
// EXAM CARD COMPONENT
// ==========================================

interface ExamCardProps {
  template: ExamTemplate;
  onStart: (templateId: number) => void;
}

const ExamCard: React.FC<ExamCardProps> = ({ template, onStart }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      whileHover={{ scale: 1.03, y: -8 }}
      transition={{ duration: 0.3 }}
      className="group bg-white rounded-2xl shadow-md hover:shadow-2xl transition-all duration-300 overflow-hidden border border-slate-100"
    >
      {/* Card Header with Icon */}
      <div className="relative bg-gradient-to-br from-cyan-500 via-teal-500 to-emerald-500 p-5">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="w-10 h-10 bg-white/20 backdrop-blur-sm rounded-lg flex items-center justify-center mb-3 group-hover:scale-110 transition-transform duration-300">
              <span className="text-xl">📝</span>
            </div>
            <h3 className="text-xl font-bold text-white mb-1 line-clamp-2">{template.name}</h3>
          </div>
        </div>
      </div>

      {/* Card Body */}
      <div className="p-5">
        {/* Tags */}
        <div className="flex flex-wrap gap-2 mb-4">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-cyan-50 text-cyan-700 rounded-lg text-sm font-semibold border border-cyan-100">
            📚 {template.subject}
          </span>
          {template.chapter && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-teal-50 text-teal-700 rounded-lg text-sm font-semibold border border-teal-100">
              📖 {template.chapter}
            </span>
          )}
          {template.adaptiveEnabled && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-amber-50 to-orange-50 text-orange-700 rounded-lg text-sm font-semibold border border-orange-100">
              ⚡ Adaptive
            </span>
          )}
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 gap-3 mb-5">
          <div className="flex items-center gap-2 bg-gradient-to-br from-cyan-50 to-teal-50 rounded-lg p-3 border border-cyan-100">
            <div className="flex-shrink-0 w-8 h-8 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-lg flex items-center justify-center shadow-md">
              <span className="text-white text-sm">📋</span>
            </div>
            <div>
              <div className="text-xs font-medium text-cyan-600 mb-0.5">Questions</div>
              <div className="text-2xl font-bold text-cyan-900">{template.totalQuestions}</div>
            </div>
          </div>
          <div className="flex items-center gap-2 bg-gradient-to-br from-teal-50 to-emerald-50 rounded-lg p-3 border border-teal-100">
            <div className="flex-shrink-0 w-8 h-8 bg-gradient-to-br from-teal-500 to-emerald-600 rounded-lg flex items-center justify-center shadow-md">
              <span className="text-white text-sm">⏱️</span>
            </div>
            <div>
              <div className="text-xs font-medium text-teal-600 mb-0.5">Duration</div>
              <div className="text-2xl font-bold text-teal-900">{template.durationMinutes}m</div>
            </div>
          </div>
        </div>

        {/* CTA Button */}
        <button
          onClick={() => onStart(template.id)}
          className="group/btn w-full px-5 py-3 bg-gradient-to-r from-cyan-500 via-teal-500 to-emerald-500 text-white rounded-lg font-bold hover:shadow-xl hover:shadow-cyan-500/30 transition-all duration-300 hover:scale-[1.02] flex items-center justify-center gap-2"
        >
          <span>Start Practice</span>
          <span className="group-hover/btn:translate-x-1 transition-transform duration-300">▶️</span>
        </button>
      </div>
    </motion.div>
  );
};

// ==========================================
// HISTORY CARD COMPONENT
// ==========================================

interface HistoryCardProps {
  history: ExamHistory;
  onViewResult: (attemptId: number) => void;
}

const HistoryCard: React.FC<HistoryCardProps> = ({ history, onViewResult }) => {
  const passed = history.scorePercent >= 60;
  
  return (
    <motion.div
      whileHover={{ scale: 1.02, y: -4 }}
      transition={{ duration: 0.2 }}
      className="group bg-white rounded-xl shadow-md hover:shadow-xl transition-all duration-300 overflow-hidden border border-slate-100"
    >
      {/* Score Badge Header */}
      <div className={`px-4 py-3 ${passed ? 'bg-gradient-to-r from-emerald-50 to-green-50 border-b border-emerald-100' : 'bg-gradient-to-r from-amber-50 to-orange-50 border-b border-amber-100'}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className={`w-8 h-8 rounded-lg flex items-center justify-center shadow-sm ${passed ? 'bg-gradient-to-br from-emerald-500 to-green-600' : 'bg-gradient-to-br from-amber-500 to-orange-600'}`}>
              <span className="text-white text-sm">{passed ? '✓' : '!'}</span>
            </div>
            <div>
              <div className={`text-xs font-semibold uppercase tracking-wide ${passed ? 'text-emerald-700' : 'text-amber-700'}`}>
                {passed ? 'Great Job!' : 'Keep Practicing'}
              </div>
              <div className={`text-2xl font-bold ${passed ? 'text-emerald-600' : 'text-amber-600'}`}>
                {history.scorePercent.toFixed(0)}%
              </div>
            </div>
          </div>
          <div className={`text-xs font-medium px-2 py-1 rounded-full ${passed ? 'bg-emerald-200 text-emerald-800' : 'bg-amber-200 text-amber-800'}`}>
            {history.correctCount}/{history.correctCount + history.wrongCount} correct
          </div>
        </div>
      </div>

      {/* Card Content */}
      <div className="p-4">
        <h4 className="font-bold text-slate-900 mb-2 line-clamp-1 group-hover:text-cyan-600 transition-colors">
          {history.examName}
        </h4>
        <div className="flex flex-wrap gap-1.5 mb-2">
          <span className="inline-flex items-center gap-1 px-2 py-1 bg-cyan-50 text-cyan-700 rounded-md text-xs font-semibold border border-cyan-100">
            📚 {history.subject}
          </span>
          {history.chapter && (
            <span className="inline-flex items-center gap-1 px-2 py-1 bg-teal-50 text-teal-700 rounded-md text-xs font-semibold border border-teal-100">
              📖 {history.chapter}
            </span>
          )}
        </div>
        <div className="flex items-center gap-1.5 text-xs text-slate-500 mb-3">
          <span>📅</span>
          {new Date(history.startedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} at{' '}
          {new Date(history.startedAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}
        </div>
        <button
          onClick={() => onViewResult(history.attemptId)}
          className="w-full px-4 py-2.5 bg-gradient-to-r from-cyan-500 to-teal-600 hover:from-cyan-600 hover:to-teal-700 text-white rounded-lg text-sm font-semibold shadow-md hover:shadow-lg transition-all duration-200 flex items-center justify-center gap-2 group-hover:scale-[1.02]"
        >
          <span>View Details</span>
          <span className="group-hover:translate-x-1 transition-transform duration-300">›</span>
        </button>
      </div>
    </motion.div>
  );
};

// ==========================================
// MAIN EXAM HUB COMPONENT
// ==========================================

type ViewMode = 'list' | 'exam' | 'result' | 'written-upload' | 'written-processing' | 'written-result';

interface WrittenSubmissionState {
  submissionId: string;
  examId: string;
  studentId: string;
}

interface ExamHubProps {
  token?: string;
  toast: ReturnType<typeof useToast>;
}

const ExamHub: React.FC<ExamHubProps> = ({ toast }) => {
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [selectedTemplateId, setSelectedTemplateId] = useState<number | null>(null);
  const [selectedAttemptId, setSelectedAttemptId] = useState<number | null>(null);
  const [templates, setTemplates] = useState<ExamTemplate[]>([]);
  const [history, setHistory] = useState<ExamHistory[]>([]);
  const [loading, setLoading] = useState(true);
  
  // Written exam state
  const [writtenSubmission, setWrittenSubmission] = useState<WrittenSubmissionState | null>(null);
  const [writtenResult, setWrittenResult] = useState<WrittenExamResult | null>(null);

  const getStudentId = (): string => {
    let studentId = localStorage.getItem('test-student-id');
    if (!studentId) {
      studentId = 'test-student-001';
      localStorage.setItem('test-student-id', studentId);
    }
    return studentId;
  };

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        const [templatesData, historyData] = await Promise.all([
          getExamTemplates(),
          getExamHistory(getStudentId())
        ]);
        setTemplates(templatesData);
        setHistory(historyData);
      } catch (err) {
        console.error('Failed to load exam data:', err);
        toast.showToast('Failed to load exam data', 'error');
      } finally {
        setLoading(false);
      }
    };

    if (viewMode === 'list') {
      loadData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [viewMode]);

  const handleStartExam = (templateId: number) => {
    setSelectedTemplateId(templateId);
    setViewMode('exam');
  };

  const handleNavigateToResult = (attemptId: number) => {
    setSelectedAttemptId(attemptId);
    setViewMode('result');
  };

  const handleBackToHome = () => {
    setViewMode('list');
    setSelectedTemplateId(null);
    setSelectedAttemptId(null);
    setWrittenSubmission(null);
    setWrittenResult(null);
  };

  // Written exam handlers
  const handleStartWrittenUpload = () => {
    setViewMode('written-upload');
  };

  const handleWrittenUploadSuccess = (submissionId: string, examId: string, studentId: string) => {
    setWrittenSubmission({ submissionId, examId, studentId });
    setViewMode('written-processing');
  };

  const handleWrittenProcessingComplete = (result: WrittenExamResult) => {
    setWrittenResult(result);
    setViewMode('written-result');
  };

  // Render exam page
  if (viewMode === 'exam' && selectedTemplateId) {
    return (
      <ExamPage
        examTemplateId={selectedTemplateId}
        onNavigateToResult={handleNavigateToResult}
        toast={toast}
      />
    );
  }

  // Render result page
  if (viewMode === 'result' && selectedAttemptId) {
    return (
      <ExamResult
        attemptId={selectedAttemptId}
        onBackToHome={handleBackToHome}
        toast={toast}
      />
    );
  }

  // Render written exam upload page
  if (viewMode === 'written-upload') {
    return (
      <WrittenExamUpload
        onSuccess={handleWrittenUploadSuccess}
        onBack={handleBackToHome}
        toast={toast}
      />
    );
  }

  // Render written exam processing status
  if (viewMode === 'written-processing' && writtenSubmission) {
    return (
      <ProcessingStatus
        writtenSubmissionId={writtenSubmission.submissionId}
        examId={writtenSubmission.examId}
        studentId={writtenSubmission.studentId}
        onComplete={handleWrittenProcessingComplete}
        onBack={handleBackToHome}
        toast={toast}
      />
    );
  }

  // Render written exam result
  if (viewMode === 'written-result' && writtenResult) {
    return (
      <WrittenExamResultView
        result={writtenResult}
        onBack={handleBackToHome}
      />
    );
  }

  // Render exam list
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Modern Hero Section */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-12"
        >
          <div className="page-header-icon mx-auto">
            <span className="text-2xl sm:text-3xl">📝</span>
          </div>
          <h1 className="page-header-title text-gradient">
            Practice Hub
          </h1>
          <p className="page-header-subtitle">
            Practice with exams designed just for your syllabus
          </p>
          <div className="flex justify-center mt-3">
            <span className="exam-safe-badge">
              <span>🔒</span>
              100% Syllabus-Based Questions
            </span>
          </div>
        </motion.div>

        {/* Written Answer Upload Banner */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-8"
        >
          <div className="bg-gradient-to-r from-cyan-500 via-teal-500 to-emerald-500 rounded-2xl shadow-xl overflow-hidden">
            <div className="p-6 md:p-8 flex flex-col md:flex-row items-center justify-between gap-6">
              <div className="flex items-center gap-4 text-white">
                <div className="w-12 h-12 bg-white/20 backdrop-blur-sm rounded-xl flex items-center justify-center">
                  <span className="text-2xl">📷</span>
                </div>
                <div>
                  <h3 className="text-xl font-bold">Upload Written Answers</h3>
                  <p className="text-white/80 text-sm">Take a photo of your handwritten answers for AI evaluation</p>
                </div>
              </div>
              <motion.button
                onClick={handleStartWrittenUpload}
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                className="px-6 py-3 bg-white text-cyan-600 rounded-xl font-bold shadow-lg hover:shadow-xl transition-all flex items-center gap-2"
              >
                <span>📤</span>
                Upload Now
              </motion.button>
            </div>
          </div>
        </motion.div>

        {loading ? (
          <div className="text-center py-20">
            <div className="relative w-16 h-16 mx-auto mb-6">
              <div className="absolute inset-0 border-4 border-slate-200 rounded-full"></div>
              <div className="absolute inset-0 border-4 border-cyan-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
            <p className="text-lg text-slate-600 font-medium">Loading practice exams...</p>
          </div>
        ) : (
          <div className="grid lg:grid-cols-3 gap-8">
            {/* Available Exams - Full Width Grid */}
            <div className="lg:col-span-2">
              <div className="mb-6 flex items-center justify-between">
                <h2 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
                  <div className="w-8 h-8 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-lg flex items-center justify-center shadow-md">
                    <span className="text-white text-sm">📄</span>
                  </div>
                  <span>Available Tests</span>
                </h2>
                <span className="px-4 py-2 bg-white rounded-full shadow-md text-sm font-semibold text-slate-700 border border-slate-200">
                  {templates.length} {templates.length === 1 ? 'Test' : 'Tests'}
                </span>
              </div>
              <div className="grid gap-6">
                {templates.length === 0 ? (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="empty-state"
                  >
                    <div className="empty-state-icon">📝</div>
                    <h3 className="empty-state-title">No Tests Available Yet</h3>
                    <p className="empty-state-text">
                      Practice tests will appear here once your teacher uploads question papers.
                      Check back soon!
                    </p>
                  </motion.div>
                ) : (
                  templates.map((template, index) => (
                    <motion.div
                      key={template.id}
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: index * 0.1 }}
                    >
                      <ExamCard template={template} onStart={handleStartExam} />
                    </motion.div>
                  ))
                )}
              </div>
            </div>

            {/* Exam History Sidebar */}
            <div className="lg:sticky lg:top-8 lg:self-start">
              <div className="mb-5 flex items-center gap-2">
                <div className="w-8 h-8 bg-gradient-to-br from-teal-500 to-emerald-600 rounded-lg flex items-center justify-center shadow-md">
                  <span className="text-white text-sm">📊</span>
                </div>
                <h2 className="text-2xl font-bold text-slate-900">Your Progress</h2>
              </div>
              <div className="space-y-4">
                {history.length === 0 ? (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="bg-white rounded-xl shadow-lg p-8 text-center border border-slate-100"
                  >
                    <div className="w-12 h-12 bg-gradient-to-br from-slate-100 to-slate-200 rounded-2xl flex items-center justify-center mx-auto mb-4">
                      <span className="text-2xl">🎯</span>
                    </div>
                    <h3 className="font-semibold text-slate-800 mb-1">No practice yet</h3>
                    <p className="text-sm text-slate-500">Start your first test to see your progress!</p>
                  </motion.div>
                ) : (
                  history.slice(0, 5).map((h, index) => (
                    <motion.div
                      key={h.attemptId}
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ delay: index * 0.05 }}
                    >
                      <HistoryCard history={h} onViewResult={handleNavigateToResult} />
                    </motion.div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ExamHub;
