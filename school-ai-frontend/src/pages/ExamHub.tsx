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
      className="group bg-white rounded-2xl shadow-md hover:shadow-2xl transition-all duration-300 overflow-hidden border border-gray-100"
    >
      {/* Card Header with Icon */}
      <div className="relative bg-gradient-to-br from-indigo-500 via-purple-600 to-pink-500 p-5">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="w-10 h-10 bg-white/20 backdrop-blur-sm rounded-lg flex items-center justify-center mb-3 group-hover:scale-110 transition-transform duration-300">
              <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-white mb-1 line-clamp-2">{template.name}</h3>
          </div>
        </div>
      </div>

      {/* Card Body */}
      <div className="p-5">
        {/* Tags */}
        <div className="flex flex-wrap gap-2 mb-4">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-indigo-50 text-indigo-700 rounded-lg text-sm font-semibold border border-indigo-100">
            <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3zM3.31 9.397L5 10.12v4.102a8.969 8.969 0 00-1.05-.174 1 1 0 01-.89-.89 11.115 11.115 0 01.25-3.762zM9.3 16.573A9.026 9.026 0 007 14.935v-3.957l1.818.78a3 3 0 002.364 0l5.508-2.361a11.026 11.026 0 01.25 3.762 1 1 0 01-.89.89 8.968 8.968 0 00-5.35 2.524 1 1 0 01-1.4 0zM6 18a1 1 0 001-1v-2.065a8.935 8.935 0 00-2-.712V17a1 1 0 001 1z"/>
            </svg>
            {template.subject}
          </span>
          {template.chapter && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-purple-50 text-purple-700 rounded-lg text-sm font-semibold border border-purple-100">
              <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 4.804A7.968 7.968 0 005.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 015.5 14c1.669 0 3.218.51 4.5 1.385A7.962 7.962 0 0114.5 14c1.255 0 2.443.29 3.5.804v-10A7.968 7.968 0 0014.5 4c-1.255 0-2.443.29-3.5.804V12a1 1 0 11-2 0V4.804z"/>
              </svg>
              {template.chapter}
            </span>
          )}
          {template.adaptiveEnabled && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-yellow-50 to-orange-50 text-orange-700 rounded-lg text-sm font-semibold border border-orange-100">
              <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M11.3 1.046A1 1 0 0112 2v5h4a1 1 0 01.82 1.573l-7 10A1 1 0 018 18v-5H4a1 1 0 01-.82-1.573l7-10a1 1 0 011.12-.38z" clipRule="evenodd"/>
              </svg>
              Adaptive
            </span>
          )}
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 gap-3 mb-5">
          <div className="flex items-center gap-2 bg-gradient-to-br from-blue-50 to-indigo-50 rounded-lg p-3 border border-indigo-100">
            <div className="flex-shrink-0 w-8 h-8 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center shadow-md">
              <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z" />
                <path fillRule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clipRule="evenodd" />
              </svg>
            </div>
            <div>
              <div className="text-xs font-medium text-indigo-600 mb-0.5">Questions</div>
              <div className="text-2xl font-bold text-indigo-900">{template.totalQuestions}</div>
            </div>
          </div>
          <div className="flex items-center gap-2 bg-gradient-to-br from-purple-50 to-pink-50 rounded-lg p-3 border border-purple-100">
            <div className="flex-shrink-0 w-8 h-8 bg-gradient-to-br from-purple-500 to-pink-600 rounded-lg flex items-center justify-center shadow-md">
              <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
              </svg>
            </div>
            <div>
              <div className="text-xs font-medium text-purple-600 mb-0.5">Duration</div>
              <div className="text-2xl font-bold text-purple-900">{template.durationMinutes}m</div>
            </div>
          </div>
        </div>

        {/* CTA Button */}
        <button
          onClick={() => onStart(template.id)}
          className="group/btn w-full px-5 py-3 bg-gradient-to-r from-indigo-500 via-purple-600 to-pink-500 text-white rounded-lg font-bold hover:shadow-xl hover:shadow-purple-500/30 transition-all duration-300 hover:scale-[1.02] flex items-center justify-center gap-2"
        >
          <span>Start Exam</span>
          <svg className="w-4 h-4 group-hover/btn:translate-x-1 transition-transform duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
          </svg>
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
      className="group bg-white rounded-xl shadow-md hover:shadow-xl transition-all duration-300 overflow-hidden border border-gray-100"
    >
      {/* Score Badge Header */}
      <div className={`px-4 py-3 ${passed ? 'bg-gradient-to-r from-green-50 to-emerald-50 border-b border-green-100' : 'bg-gradient-to-r from-orange-50 to-amber-50 border-b border-orange-100'}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className={`w-8 h-8 rounded-lg flex items-center justify-center shadow-sm ${passed ? 'bg-gradient-to-br from-green-500 to-emerald-600' : 'bg-gradient-to-br from-orange-500 to-amber-600'}`}>
              <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                {passed ? (
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                ) : (
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                )}
              </svg>
            </div>
            <div>
              <div className={`text-xs font-semibold uppercase tracking-wide ${passed ? 'text-green-700' : 'text-orange-700'}`}>
                {passed ? 'Passed' : 'Needs Work'}
              </div>
              <div className={`text-2xl font-bold ${passed ? 'text-green-600' : 'text-orange-600'}`}>
                {history.scorePercent.toFixed(0)}%
              </div>
            </div>
          </div>
          <div className={`text-xs font-medium px-2 py-1 rounded-full ${passed ? 'bg-green-200 text-green-800' : 'bg-orange-200 text-orange-800'}`}>
            {history.correctCount}/{history.correctCount + history.wrongCount}
          </div>
        </div>
      </div>

      {/* Card Content */}
      <div className="p-4">
        <h4 className="font-bold text-gray-900 mb-2 line-clamp-1 group-hover:text-indigo-600 transition-colors">
          {history.examName}
        </h4>
        <div className="flex flex-wrap gap-1.5 mb-2">
          <span className="inline-flex items-center gap-1 px-2 py-1 bg-indigo-50 text-indigo-700 rounded-md text-xs font-semibold border border-indigo-100">
            <svg className="w-2.5 h-2.5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3z"/>
            </svg>
            {history.subject}
          </span>
          {history.chapter && (
            <span className="inline-flex items-center gap-1 px-2 py-1 bg-purple-50 text-purple-700 rounded-md text-xs font-semibold border border-purple-100">
              <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 4.804A7.968 7.968 0 005.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 015.5 14c1.669 0 3.218.51 4.5 1.385A7.962 7.962 0 0114.5 14c1.255 0 2.443.29 3.5.804v-10A7.968 7.968 0 0014.5 4c-1.255 0-2.443.29-3.5.804V12a1 1 0 11-2 0V4.804z"/>
              </svg>
              {history.chapter}
            </span>
          )}
        </div>
        <div className="flex items-center gap-1.5 text-xs text-gray-500 mb-3">
          <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
          </svg>
          {new Date(history.startedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} at{' '}
          {new Date(history.startedAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}
        </div>
        <button
          onClick={() => onViewResult(history.attemptId)}
          className="w-full px-4 py-2.5 bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white rounded-lg text-sm font-semibold shadow-md hover:shadow-lg transition-all duration-200 flex items-center justify-center gap-2 group-hover:scale-[1.02]"
        >
          <span>View Details</span>
          <svg className="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
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
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-indigo-50 to-purple-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Modern Hero Section */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-12"
        >
          <div className="inline-flex items-center justify-center w-12 h-12 sm:w-16 sm:h-16 bg-gradient-to-br from-indigo-500 via-purple-600 to-pink-500 rounded-2xl shadow-xl shadow-purple-500/20 mb-6">
            <svg className="w-6 h-6 sm:w-8 sm:h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
            </svg>
          </div>
          <h1 className="text-4xl lg:text-5xl font-extrabold bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 bg-clip-text text-transparent mb-3 leading-tight">
            Available Exams
          </h1>
          <p className="text-lg text-gray-600 max-w-2xl mx-auto leading-relaxed">
            Test your knowledge with adaptive difficulty exams designed to match your skill level
          </p>
        </motion.div>

        {/* Written Answer Upload Banner */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-8"
        >
          <div className="bg-gradient-to-r from-purple-600 via-pink-600 to-red-500 rounded-2xl shadow-xl overflow-hidden">
            <div className="p-6 md:p-8 flex flex-col md:flex-row items-center justify-between gap-6">
              <div className="flex items-center gap-4 text-white">
                <div className="w-12 h-12 bg-white/20 backdrop-blur-sm rounded-xl flex items-center justify-center">
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
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
                className="px-6 py-3 bg-white text-purple-600 rounded-xl font-bold shadow-lg hover:shadow-xl transition-all flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                </svg>
                Upload Now
              </motion.button>
            </div>
          </div>
        </motion.div>

        {loading ? (
          <div className="text-center py-20">
            <div className="relative w-16 h-16 mx-auto mb-6">
              <div className="absolute inset-0 border-4 border-indigo-200 rounded-full"></div>
              <div className="absolute inset-0 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
            </div>
            <p className="text-lg text-gray-600 font-medium">Loading exams...</p>
          </div>
        ) : (
          <div className="grid lg:grid-cols-3 gap-8">
            {/* Available Exams - Full Width Grid */}
            <div className="lg:col-span-2">
              <div className="mb-6 flex items-center justify-between">
                <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
                  <div className="w-8 h-8 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center shadow-md">
                    <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                  </div>
                  <span>All Exams</span>
                </h2>
                <span className="px-4 py-2 bg-white rounded-full shadow-md text-sm font-semibold text-gray-700 border border-gray-200">
                  {templates.length} {templates.length === 1 ? 'Exam' : 'Exams'}
                </span>
              </div>
              <div className="grid gap-6">
                {templates.length === 0 ? (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="bg-white rounded-2xl shadow-lg p-12 text-center border border-gray-100"
                  >
                    <div className="w-12 h-12 bg-gradient-to-br from-gray-100 to-gray-200 rounded-2xl flex items-center justify-center mx-auto mb-6">
                      <svg className="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                      </svg>
                    </div>
                    <h3 className="text-xl font-bold text-gray-800 mb-2">No exams available</h3>
                    <p className="text-gray-600">Check back later for new exams</p>
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
                <div className="w-8 h-8 bg-gradient-to-br from-purple-500 to-pink-600 rounded-lg flex items-center justify-center shadow-md">
                  <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
                  </svg>
                </div>
                <h2 className="text-2xl font-bold text-gray-900">Recent History</h2>
              </div>
              <div className="space-y-4">
                {history.length === 0 ? (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="bg-white rounded-xl shadow-lg p-8 text-center border border-gray-100"
                  >
                    <div className="w-12 h-12 bg-gradient-to-br from-gray-100 to-gray-200 rounded-2xl flex items-center justify-center mx-auto mb-4">
                      <svg className="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
                      </svg>
                    </div>
                    <h3 className="font-semibold text-gray-800 mb-1">No history yet</h3>
                    <p className="text-sm text-gray-500">Start your first exam!</p>
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
