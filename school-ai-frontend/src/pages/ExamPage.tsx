import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useToast } from "../hooks/useToast";

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface QuestionOption {
  id: number;
  optionText: string;
}

interface Question {
  id: number;
  subject: string;
  chapter: string | null;
  topic: string | null;
  text: string;
  type: string;
  difficulty: string;
  options: QuestionOption[];
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

interface CurrentStats {
  answeredCount: number;
  correctCount: number;
  wrongCount: number;
  currentAccuracy: number;
}

interface StartExamResponse {
  attemptId: number;
  template: ExamTemplate;
  firstQuestion: Question | null;
}

interface SubmitAnswerResponse {
  isCorrect: boolean;
  isCompleted: boolean;
  nextQuestion: Question | null;
  currentStats: CurrentStats;
}

// ==========================================
// API FUNCTIONS
// ==========================================

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

async function startExam(studentId: string, examTemplateId: number): Promise<StartExamResponse> {
  const response = await fetch(`${API_URL}/api/exam/start`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ studentId, examTemplateId }),
  });
  
  if (!response.ok) {
    throw new Error(`Failed to start exam: ${response.statusText}`);
  }
  
  return response.json();
}

async function submitAnswer(
  attemptId: number,
  questionId: number,
  selectedOptionId: number,
  timeTakenSeconds: number
): Promise<SubmitAnswerResponse> {
  const response = await fetch(`${API_URL}/api/exam/${attemptId}/answer`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ questionId, selectedOptionId, timeTakenSeconds }),
  });
  
  if (!response.ok) {
    throw new Error(`Failed to submit answer: ${response.statusText}`);
  }
  
  return response.json();
}

// ==========================================
// TOP NAVIGATION BAR
// ==========================================

interface NavBarProps {
  examName: string;
  onLogout: () => void;
}

const NavBar: React.FC<NavBarProps> = ({ examName, onLogout }) => {
  const [showExamMenu, setShowExamMenu] = useState(false);

  return (
    <nav className="sticky top-0 z-50 bg-white border-b border-gray-200 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between h-14">
          {/* Left: Logo & Exam Name */}
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3z"/>
              </svg>
            </div>
            <span className="text-base font-semibold text-gray-900">Exam Portal</span>
          </div>

          {/* Center: Exam Dropdown */}
          <div className="relative">
            <button
              onClick={() => setShowExamMenu(!showExamMenu)}
              className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50 rounded-lg transition-colors"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <span className="max-w-xs truncate">{examName}</span>
              <svg className={`w-4 h-4 transition-transform ${showExamMenu ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            {showExamMenu && (
              <div className="absolute top-full mt-1 left-0 w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-1">
                <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wide">Current Exam</div>
                <div className="px-3 py-2 text-sm text-gray-700">{examName}</div>
              </div>
            )}
          </div>

          {/* Right: Logout */}
          <button
            onClick={onLogout}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 rounded-lg transition-colors"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            Logout
          </button>
        </div>
      </div>
    </nav>
  );
};

// ==========================================
// SIDEBAR COMPONENT
// ==========================================

interface SidebarProps {
  currentQuestionNumber: number;
  totalQuestions: number;
  markedForReview: boolean;
  onToggleReview: () => void;
  difficulty: string;
  mobileOpen: boolean;
  onMobileToggle: () => void;
}

const Sidebar: React.FC<SidebarProps> = ({ 
  currentQuestionNumber, 
  totalQuestions, 
  markedForReview, 
  onToggleReview, 
  difficulty,
  mobileOpen,
  onMobileToggle
}) => {
  const sidebarItems = [
    {
      icon: (
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6z"/>
        </svg>
      ),
      label: "Question",
      value: `${currentQuestionNumber}/${totalQuestions}`,
      active: true
    },
    {
      icon: (
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd"/>
        </svg>
      ),
      label: "Timer",
      value: "Active"
    }
  ];

  return (
    <>
      {/* Mobile Overlay */}
      {mobileOpen && (
        <div 
          className="fixed inset-0 bg-black/30 z-40 md:hidden"
          onClick={onMobileToggle}
        />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed md:sticky top-14 left-0 h-[calc(100vh-3.5rem)] w-44 bg-white border-r border-gray-200 
        flex flex-col gap-2 p-3 z-40 transition-transform duration-300
        ${mobileOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
      `}>
        {/* Question Number Card */}
        <div className="bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl p-4 text-white shadow-md">
          <div className="text-xs font-medium opacity-90 mb-1">Question</div>
          <div className="text-3xl font-bold">{currentQuestionNumber}</div>
          <div className="text-xs opacity-80 mt-1">of {totalQuestions}</div>
        </div>

        {/* Sidebar Items */}
        {sidebarItems.map((item, index) => (
          <div
            key={index}
            className={`flex flex-col items-center justify-center p-3 rounded-lg transition-all ${
              item.active 
                ? 'bg-blue-50 border-2 border-blue-600' 
                : 'bg-gray-50 hover:bg-gray-100'
            }`}
          >
            <div className={`${item.active ? 'text-blue-600' : 'text-gray-600'} mb-1.5`}>
              {item.icon}
            </div>
            <div className={`text-xs font-medium ${item.active ? 'text-blue-600' : 'text-gray-600'}`}>
              {item.label}
            </div>
            {item.value && (
              <div className="text-xs text-gray-500 mt-0.5">{item.value}</div>
            )}
          </div>
        ))}

        {/* Mark for Review */}
        <button
          onClick={onToggleReview}
          className={`flex flex-col items-center justify-center p-3 rounded-lg transition-all ${
            markedForReview
              ? 'bg-amber-50 border-2 border-amber-500'
              : 'bg-gray-50 hover:bg-gray-100 border border-gray-200'
          }`}
        >
          <svg 
            className={`w-4 h-4 mb-1.5 ${markedForReview ? 'text-amber-600' : 'text-gray-600'}`}
            fill={markedForReview ? "currentColor" : "none"}
            stroke="currentColor"
            viewBox="0 0 24 24"
            strokeWidth={2}
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
          </svg>
          <div className={`text-xs font-medium text-center ${markedForReview ? 'text-amber-600' : 'text-gray-600'}`}>
            Mark Review
          </div>
        </button>

        {/* Difficulty Badge */}
        <div className={`p-3 rounded-lg text-center text-xs font-bold ${
          difficulty === 'Easy' ? 'bg-green-100 text-green-700' :
          difficulty === 'Medium' ? 'bg-yellow-100 text-yellow-700' :
          'bg-red-100 text-red-700'
        }`}>
          {difficulty}
        </div>
      </aside>
    </>
  );
};

// ==========================================
// TIMER & PROGRESS HEADER
// ==========================================

interface TimerHeaderProps {
  durationMinutes: number;
  questionTime: number;
  onTimeUp: () => void;
  currentQuestion: number;
  totalQuestions: number;
  stats: CurrentStats;
}

const TimerHeader: React.FC<TimerHeaderProps> = ({ 
  durationMinutes, 
  questionTime, 
  onTimeUp,
  currentQuestion,
  totalQuestions,
  stats
}) => {
  const [timeLeft, setTimeLeft] = useState(durationMinutes * 60);

  useEffect(() => {
    if (timeLeft <= 0) {
      onTimeUp();
      return;
    }

    const timer = setInterval(() => {
      setTimeLeft((prev) => prev - 1);
    }, 1000);

    return () => clearInterval(timer);
  }, [timeLeft, onTimeUp]);

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;
  const isLowTime = timeLeft < 300;

  return (
    <div className={`sticky top-14 z-30 transition-all duration-300 ${
      isLowTime ? 'bg-red-500' : 'bg-blue-600'
    }`}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-3">
        {/* Timer & Stats Row */}
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-4">
            <div className="bg-white/20 backdrop-blur-sm rounded-lg px-3 py-1.5">
              <div className={`text-xl font-mono font-bold text-white tracking-wider ${isLowTime ? 'animate-pulse' : ''}`}>
                {String(minutes).padStart(2, '0')}:{String(seconds).padStart(2, '0')}
              </div>
            </div>
            {stats.answeredCount > 0 && (
              <div className="text-white text-sm">
                <span className="font-semibold">{stats.currentAccuracy.toFixed(0)}%</span>
                <span className="opacity-80 ml-1">accuracy</span>
              </div>
            )}
          </div>

          <div className="text-white text-sm font-medium hidden sm:block">
            Question {currentQuestion} of {totalQuestions}
          </div>
        </div>

        {/* Progress Bar */}
        <div className="w-full h-1.5 bg-white/20 rounded-full overflow-hidden">
          <motion.div
            className="h-full bg-white rounded-full shadow-lg"
            initial={{ width: 0 }}
            animate={{ width: `${(stats.answeredCount / totalQuestions) * 100}%` }}
            transition={{ duration: 0.5, ease: "easeOut" }}
          />
        </div>
      </div>
    </div>
  );
};

// ==========================================
// MAIN EXAM PAGE COMPONENT
// ==========================================

interface ExamPageProps {
  examTemplateId: number;
  onNavigateToResult?: (attemptId: number) => void;
  toast: ReturnType<typeof useToast>;
}

const ExamPage: React.FC<ExamPageProps> = ({ examTemplateId, onNavigateToResult, toast }) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [examData, setExamData] = useState<StartExamResponse | null>(null);
  const [currentQuestion, setCurrentQuestion] = useState<Question | null>(null);
  const [selectedOptionId, setSelectedOptionId] = useState<number | null>(null);
  const [stats, setStats] = useState<CurrentStats>({
    answeredCount: 0,
    correctCount: 0,
    wrongCount: 0,
    currentAccuracy: 0,
  });
  const [submitting, setSubmitting] = useState(false);
  const [questionStartTime, setQuestionStartTime] = useState<number>(Date.now());
  const [questionTime, setQuestionTime] = useState<number>(0);
  const [markedForReview, setMarkedForReview] = useState<boolean>(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  useEffect(() => {
    const timer = setInterval(() => {
      setQuestionTime(Math.floor((Date.now() - questionStartTime) / 1000));
    }, 1000);
    
    return () => clearInterval(timer);
  }, [questionStartTime]);

  const getStudentId = (): string => {
    let studentId = localStorage.getItem('test-student-id');
    if (!studentId) {
      studentId = 'test-student-001';
      localStorage.setItem('test-student-id', studentId);
    }
    return studentId;
  };

  useEffect(() => {
    let cancelled = false;
    
    const initExam = async () => {
      try {
        setLoading(true);
        const studentId = getStudentId();
        const response = await startExam(studentId, examTemplateId);
        
        if (!cancelled) {
          setExamData(response);
          setCurrentQuestion(response.firstQuestion);
          setQuestionStartTime(Date.now());
          setError(null);
          
          toast.showToast('Exam started successfully!', 'success');
        }
      } catch (err) {
        if (!cancelled) {
          const errorMsg = err instanceof Error ? err.message : 'Failed to start exam';
          setError(errorMsg);
          toast.showToast(errorMsg, 'error');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    initExam();
    
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [examTemplateId]);

  const handleTimeUp = () => {
    toast.showToast('Time is up! Exam will be submitted.', 'warning');
    if (examData?.attemptId) {
      onNavigateToResult?.(examData.attemptId);
    }
  };

  const handleSubmitAnswer = async () => {
    if (!selectedOptionId || !currentQuestion || !examData) {
      toast.showToast('Please select an answer', 'warning');
      return;
    }

    try {
      setSubmitting(true);
      const timeTaken = Math.floor((Date.now() - questionStartTime) / 1000);
      
      const response = await submitAnswer(
        examData.attemptId,
        currentQuestion.id,
        selectedOptionId,
        timeTaken
      );

      setStats(response.currentStats);

      if (response.isCorrect) {
        toast.showToast('✓ Correct!', 'success');
      } else {
        toast.showToast('✗ Incorrect', 'error');
      }

      if (response.isCompleted) {
        toast.showToast('Exam completed!', 'success');
        setTimeout(() => {
          onNavigateToResult?.(examData.attemptId);
        }, 1000);
        return;
      }

      setCurrentQuestion(response.nextQuestion);
      setSelectedOptionId(null);
      setQuestionStartTime(Date.now());
      setQuestionTime(0);
      setMarkedForReview(false);
      
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to submit answer';
      toast.showToast(errorMsg, 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleLogout = () => {
    if (window.confirm('Are you sure you want to logout? Your progress will be saved.')) {
      window.location.href = '/';
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-base text-gray-600 font-medium">Loading exam...</p>
        </div>
      </div>
    );
  }

  if (error || !examData || !currentQuestion) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-xl shadow-lg p-6 text-center border border-gray-200">
          <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-3">
            <svg className="w-6 h-6 text-red-600" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-800 mb-2">Error Loading Exam</h2>
          <p className="text-sm text-gray-600 mb-4">{error || 'No question available'}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-blue-600 text-white text-sm rounded-lg font-medium hover:bg-blue-700 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navigation */}
      <NavBar examName={examData.template.name} onLogout={handleLogout} />

      {/* Timer & Progress Header */}
      <TimerHeader
        durationMinutes={examData.template.durationMinutes}
        questionTime={questionTime}
        onTimeUp={handleTimeUp}
        currentQuestion={stats.answeredCount + 1}
        totalQuestions={examData.template.totalQuestions}
        stats={stats}
      />

      {/* Mobile Hamburger */}
      <button
        onClick={() => setMobileSidebarOpen(!mobileSidebarOpen)}
        className="md:hidden fixed top-16 left-4 z-50 w-10 h-10 bg-blue-600 text-white rounded-lg flex items-center justify-center shadow-lg"
      >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>

      {/* Main Content */}
      <div className="flex">
        {/* Sidebar */}
        <Sidebar
          currentQuestionNumber={stats.answeredCount + 1}
          totalQuestions={examData.template.totalQuestions}
          markedForReview={markedForReview}
          onToggleReview={() => setMarkedForReview(!markedForReview)}
          difficulty={currentQuestion.difficulty}
          mobileOpen={mobileSidebarOpen}
          onMobileToggle={() => setMobileSidebarOpen(false)}
        />

        {/* Question Area */}
        <main className="flex-1 p-4 sm:p-6 pb-24">
          <div className="max-w-4xl mx-auto">
            <AnimatePresence mode="wait">
              <motion.div
                key={currentQuestion.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                transition={{ duration: 0.3 }}
                className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 sm:p-8"
              >
                {/* Question Header */}
                <div className="mb-6">
                  <div className="flex items-center justify-between mb-3">
                    <div>
                      <h3 className="text-sm font-semibold text-blue-600 uppercase tracking-wide">
                        {currentQuestion.subject}
                      </h3>
                      {currentQuestion.chapter && (
                        <p className="text-xs text-gray-500 mt-0.5">{currentQuestion.chapter}</p>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-medium text-gray-500">
                        Question {stats.answeredCount + 1}
                      </span>
                      <span className={`px-2 py-1 rounded text-xs font-bold ${
                        currentQuestion.difficulty === 'Easy' ? 'bg-green-100 text-green-700' :
                        currentQuestion.difficulty === 'Medium' ? 'bg-yellow-100 text-yellow-700' :
                        'bg-red-100 text-red-700'
                      }`}>
                        {currentQuestion.difficulty}
                      </span>
                    </div>
                  </div>
                  <div className="h-px bg-gray-200 mb-4"></div>
                </div>

                {/* Question Text */}
                <div className="mb-8">
                  <h2 className="text-lg sm:text-xl font-bold text-gray-900 leading-relaxed tracking-normal">
                    {currentQuestion.text}
                  </h2>
                </div>

                {/* Options */}
                <div className="space-y-3">
                  {currentQuestion.options.map((option, index) => {
                    const isSelected = selectedOptionId === option.id;
                    const optionLetter = String.fromCharCode(65 + index);
                    
                    return (
                      <motion.button
                        key={option.id}
                        onClick={() => setSelectedOptionId(option.id)}
                        whileHover={{ scale: 1.01 }}
                        whileTap={{ scale: 0.98 }}
                        className={`w-full flex items-center gap-3 p-4 rounded-lg border-2 transition-all text-left ${
                          isSelected
                            ? 'border-blue-600 bg-blue-50 shadow-sm'
                            : 'border-gray-200 bg-white hover:border-gray-300 hover:bg-gray-50'
                        }`}
                      >
                        <div className={`flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center font-bold text-base transition-all ${
                          isSelected
                            ? 'bg-blue-600 text-white'
                            : 'bg-gray-100 text-gray-600'
                        }`}>
                          {optionLetter}
                        </div>
                        
                        <div className={`flex-1 text-base leading-relaxed ${
                          isSelected ? 'text-gray-900 font-medium' : 'text-gray-700'
                        }`}>
                          {option.optionText}
                        </div>
                        
                        {isSelected && (
                          <motion.div
                            initial={{ scale: 0 }}
                            animate={{ scale: 1 }}
                            className="flex-shrink-0 w-6 h-6 bg-green-500 rounded-full flex items-center justify-center"
                          >
                            <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                              <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                            </svg>
                          </motion.div>
                        )}
                      </motion.button>
                    );
                  })}
                </div>

                {/* Help Text */}
                {!selectedOptionId && (
                  <motion.div
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="mt-6 p-3 bg-blue-50 border border-blue-200 rounded-lg"
                  >
                    <p className="text-sm text-blue-700 flex items-center gap-2">
                      <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                      </svg>
                      Please select an option to continue
                    </p>
                  </motion.div>
                )}
              </motion.div>
            </AnimatePresence>
          </div>
        </main>
      </div>

      {/* Bottom Action Bar */}
      <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 shadow-lg z-30">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 py-3">
          <div className="flex items-center justify-between">
            <div className="text-sm text-gray-600 hidden sm:block">
              <span className="font-semibold text-gray-900">{stats.correctCount}</span> correct, 
              <span className="font-semibold text-gray-900 ml-1">{stats.wrongCount}</span> wrong
            </div>
            
            <motion.button
              onClick={handleSubmitAnswer}
              disabled={!selectedOptionId || submitting}
              whileHover={{ scale: selectedOptionId && !submitting ? 1.02 : 1 }}
              whileTap={{ scale: selectedOptionId && !submitting ? 0.98 : 1 }}
              className={`flex items-center gap-2 px-6 py-2.5 rounded-lg font-semibold text-sm transition-all ${
                selectedOptionId && !submitting
                  ? 'bg-blue-600 text-white shadow-md hover:bg-blue-700 hover:shadow-lg'
                  : 'bg-gray-200 text-gray-400 cursor-not-allowed'
              }`}
            >
              {submitting ? (
                <>
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Submitting...
                </>
              ) : (
                <>
                  {stats.answeredCount + 1 < examData.template.totalQuestions ? 'Next Question' : 'Finish Exam'}
                  <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                </>
              )}
            </motion.button>

            <div className="text-sm text-gray-500 hidden sm:block">
              {stats.answeredCount + 1} / {examData.template.totalQuestions}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ExamPage;
