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
  // Try plural route first (local), fallback to singular (production)
  const routes = [`${API_URL}/api/exams/start`, `${API_URL}/api/exam/start`];
  
  for (const route of routes) {
    try {
      const response = await fetch(route, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ studentId, examTemplateId }),
      });
      
      if (response.ok) {
        return response.json();
      }
    } catch (error) {
      // Try next route
    }
  }
  
  throw new Error('Failed to start exam');
}

async function submitAnswer(
  attemptId: number,
  questionId: number,
  selectedOptionId: number,
  timeTakenSeconds: number
): Promise<SubmitAnswerResponse> {
  // Try plural route first (local), fallback to singular (production)
  const routes = [
    `${API_URL}/api/exams/${attemptId}/answer`,
    `${API_URL}/api/exam/${attemptId}/answer`
  ];
  
  for (const route of routes) {
    try {
      const response = await fetch(route, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ questionId, selectedOptionId, timeTakenSeconds }),
      });
      
      if (response.ok) {
        return response.json();
      }
    } catch (error) {
      // Try next route
    }
  }
  
  throw new Error('Failed to submit answer');
}

// ==========================================
// TIMER COMPONENT
// ==========================================

interface TimerProps {
  durationMinutes: number;
  questionTime: number;
  onTimeUp: () => void;
}

const Timer: React.FC<TimerProps> = ({ durationMinutes, questionTime, onTimeUp }) => {
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
  
  const questionMinutes = Math.floor(questionTime / 60);
  const questionSeconds = questionTime % 60;

  return (
    <div className={`w-full px-6 py-3 shadow-lg transition-all duration-500 ${
      isLowTime 
        ? 'bg-gradient-to-r from-red-500 via-red-600 to-orange-500 animate-pulse' 
        : 'bg-gradient-to-r from-cyan-500 via-teal-500 to-emerald-500'
    }`}>
      <div className="max-w-7xl mx-auto flex items-center justify-between text-white">
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-white/20 backdrop-blur-sm rounded-full flex items-center justify-center ring-2 ring-white/30 shadow-lg">
              <span className="text-2xl">📝</span>
            </div>
            <div>
              <div className="text-lg font-bold tracking-wide">Practice Test</div>
              <div className="text-xs opacity-90 flex items-center gap-1">
                <span>🔒</span> Syllabus Only
              </div>
            </div>
          </div>
        </div>
        
        <div className="flex items-center gap-6">
          <div className="text-center px-4 py-2 bg-white/10 backdrop-blur-sm rounded-lg">
            <div className={`text-2xl font-mono font-bold tracking-wider ${isLowTime ? 'animate-pulse' : ''}`}>
              {String(minutes).padStart(2, '0')}:{String(seconds).padStart(2, '0')}
            </div>
            <div className="text-xs opacity-80 mt-0.5">Time Left</div>
          </div>
          
          <div className="h-10 w-px bg-white/30"></div>
          
          <div className="text-center px-3 py-2 bg-white/5 rounded-lg">
            <div className="text-sm font-semibold">
              {String(questionMinutes).padStart(2, '0')}:{String(questionSeconds).padStart(2, '0')}
            </div>
            <div className="text-xs opacity-80">This Q</div>
          </div>
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
      
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to submit answer';
      toast.showToast(errorMsg, 'error');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-cyan-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-lg text-slate-600 font-medium">Loading your practice test...</p>
          <p className="text-sm text-slate-500 mt-2">🔒 Exam Safe • Syllabus Only</p>
        </div>
      </div>
    );
  }

  if (error || !examData || !currentQuestion) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="text-5xl mb-4">😅</div>
          <h2 className="text-2xl font-bold text-slate-800 mb-2">Oops! Something went wrong</h2>
          <p className="text-slate-600 mb-6">{error || 'No question available at the moment'}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-6 py-3 bg-gradient-to-r from-cyan-500 to-teal-500 text-white rounded-xl font-medium hover:from-cyan-600 hover:to-teal-600 shadow-lg transition-all"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 pb-24">
      <Timer 
        durationMinutes={examData.template.durationMinutes} 
        questionTime={questionTime}
        onTimeUp={handleTimeUp} 
      />

      {/* Progress Bar */}
      <div className="w-full bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-2">
          <div className="flex items-center justify-between text-sm text-slate-600 mb-2">
            <span className="font-medium">📊 Progress: {stats.answeredCount} of {examData.template.totalQuestions}</span>
            {stats.answeredCount > 0 && (
              <span className="font-semibold text-teal-600">
                Accuracy: {stats.currentAccuracy.toFixed(0)}% 
                <span className="text-slate-500 ml-2">({stats.correctCount}✓ / {stats.wrongCount}✗)</span>
              </span>
            )}
          </div>
          <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
            <motion.div 
              className="h-full bg-gradient-to-r from-cyan-500 via-teal-500 to-emerald-500 rounded-full"
              initial={{ width: 0 }}
              animate={{ width: `${(stats.answeredCount / examData.template.totalQuestions) * 100}%` }}
              transition={{ duration: 0.5, ease: "easeOut" }}
            />
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="flex gap-6">
          {/* Left Sidebar - Question Number & Actions */}
          <div className="flex-shrink-0 space-y-4">
            <motion.div 
              className="relative"
              whileHover={{ scale: 1.05 }}
              transition={{ type: "spring", stiffness: 300 }}
            >
              <div className="w-20 h-20 bg-gradient-to-br from-cyan-500 to-teal-600 text-white rounded-2xl flex items-center justify-center text-3xl font-bold shadow-lg">
                {stats.answeredCount + 1}
              </div>
              <div className="absolute -top-2 -right-2 w-7 h-7 bg-emerald-400 rounded-full flex items-center justify-center text-xs font-bold text-white shadow-md">
                {examData.template.totalQuestions}
              </div>
            </motion.div>
            
            <motion.button 
              onClick={() => setMarkedForReview(!markedForReview)}
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className={`w-20 h-20 rounded-2xl flex flex-col items-center justify-center gap-1 text-xs font-medium transition-all shadow-md ${
                markedForReview 
                  ? 'bg-gradient-to-br from-amber-400 to-orange-500 text-white' 
                  : 'bg-white text-gray-600 hover:bg-amber-50 hover:text-amber-600 border-2 border-gray-200'
              }`}
            >
              <span className="text-xl">{markedForReview ? '🔖' : '🗑️'}</span>
              <span className="text-center leading-tight">Mark<br/>Review</span>
            </motion.button>

            {/* Difficulty Badge */}
            <div className={`w-20 p-3 rounded-xl text-center text-xs font-bold shadow-md ${
              currentQuestion.difficulty === 'Easy' ? 'bg-gradient-to-br from-green-400 to-emerald-500 text-white' :
              currentQuestion.difficulty === 'Medium' ? 'bg-gradient-to-br from-yellow-400 to-amber-500 text-white' :
              'bg-gradient-to-br from-red-400 to-pink-500 text-white'
            }`}>
              {currentQuestion.difficulty}
            </div>
          </div>

          {/* Main Content Area */}
          <div className="flex-1">
            <AnimatePresence mode="wait">
              <motion.div
                key={currentQuestion.id}
                initial={{ opacity: 0, x: 50 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -50 }}
                transition={{ duration: 0.4, type: "spring" }}
                className="bg-white rounded-2xl shadow-lg p-8 border-2 border-cyan-100"
              >
                {/* Question Header */}
                <div className="flex items-center justify-between mb-6">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-lg flex items-center justify-center">
                      <span className="text-xl">❓</span>
                    </div>
                    <div>
                      <div className="text-xs text-slate-500 font-medium">Question {stats.answeredCount + 1}</div>
                      <div className="text-sm font-semibold text-slate-700">{currentQuestion.subject}</div>
                    </div>
                  </div>
                  {currentQuestion.topic && (
                    <span className="px-3 py-1 bg-teal-100 text-teal-700 rounded-full text-xs font-medium">
                      {currentQuestion.topic}
                    </span>
                  )}
                </div>

                {/* Question Text */}
                <div className="mb-8 p-6 bg-gradient-to-br from-cyan-50 to-teal-50 rounded-xl border-l-4 border-cyan-500">
                  <h2 className="text-2xl font-semibold text-slate-800 leading-relaxed">
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
                        whileHover={{ scale: 1.01, x: 4 }}
                        whileTap={{ scale: 0.99 }}
                        className={`w-full flex items-center gap-4 p-5 rounded-xl border-2 transition-all duration-300 ${
                          isSelected
                            ? 'border-cyan-500 bg-gradient-to-r from-cyan-50 to-teal-50 shadow-md'
                            : 'border-slate-200 bg-white hover:border-cyan-300 hover:shadow-sm'
                        }`}
                      >
                        <div className={`flex-shrink-0 w-12 h-12 rounded-xl flex items-center justify-center font-bold text-lg transition-all ${
                          isSelected
                            ? 'bg-gradient-to-br from-cyan-500 to-teal-600 text-white shadow-md scale-110'
                            : 'bg-slate-100 text-slate-600'
                        }`}>
                          {optionLetter}
                        </div>
                        
                        <div className={`flex-1 text-left text-lg ${
                          isSelected ? 'text-gray-900 font-medium' : 'text-gray-700'
                        }`}>
                          {option.optionText}
                        </div>
                        
                        {isSelected && (
                          <motion.div
                            initial={{ scale: 0, rotate: -180 }}
                            animate={{ scale: 1, rotate: 0 }}
                            transition={{ type: "spring", stiffness: 200 }}
                            className="flex-shrink-0 w-8 h-8 bg-green-500 rounded-full flex items-center justify-center"
                          >
                            <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
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
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    className="mt-6 p-4 bg-cyan-50 border border-cyan-200 rounded-xl"
                  >
                    <p className="text-sm text-cyan-700 flex items-center gap-2">
                      <span>💡</span>
                      Select an option to proceed to the next question
                    </p>
                  </motion.div>
                )}
              </motion.div>
            </AnimatePresence>
          </div>
        </div>
      </div>

      {/* Footer Navigation */}
      <div className="fixed bottom-0 left-0 right-0 bg-white border-t-2 border-slate-200 shadow-2xl backdrop-blur-sm">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3 text-slate-600">
              <span className="text-xl">📋</span>
              <span className="font-semibold text-lg">{examData.template.name}</span>
            </div>
            
            <motion.button
              onClick={handleSubmitAnswer}
              disabled={!selectedOptionId || submitting}
              whileHover={{ scale: selectedOptionId && !submitting ? 1.05 : 1 }}
              whileTap={{ scale: selectedOptionId && !submitting ? 0.95 : 1 }}
              className={`px-10 py-4 rounded-xl font-bold text-lg flex items-center gap-3 transition-all duration-300 ${
                selectedOptionId && !submitting
                  ? 'bg-gradient-to-r from-cyan-500 to-teal-600 text-white shadow-lg hover:shadow-xl'
                  : 'bg-slate-200 text-slate-400 cursor-not-allowed'
              }`}
            >
              {submitting ? (
                <>
                  <div className="w-6 h-6 border-3 border-white border-t-transparent rounded-full animate-spin"></div>
                  Submitting...
                </>
              ) : (
                <>
                  {stats.answeredCount + 1 < examData.template.totalQuestions ? 'Next Question' : 'Finish Exam'}
                  <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                </>
              )}
            </motion.button>

            <div className="text-sm text-slate-500">
              Question <span className="font-bold text-teal-600">{stats.answeredCount + 1}</span> of {examData.template.totalQuestions}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ExamPage;
