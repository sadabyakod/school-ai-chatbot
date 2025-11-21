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
  const response = await fetch(`${API_URL}/api/exams/start`, {
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
  const response = await fetch(`${API_URL}/api/exams/${attemptId}/answer`, {
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
        ? 'bg-gradient-to-r from-red-500 via-red-600 to-pink-600 animate-pulse' 
        : 'bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600'
    }`}>
      <div className="max-w-7xl mx-auto flex items-center justify-between text-white">
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-white/20 backdrop-blur-sm rounded-full flex items-center justify-center ring-2 ring-white/30 shadow-lg">
              <svg className="w-7 h-7 text-white" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3zM3.31 9.397L5 10.12v4.102a8.969 8.969 0 00-1.05-.174 1 1 0 01-.89-.89 11.115 11.115 0 01.25-3.762zM9.3 16.573A9.026 9.026 0 007 14.935v-3.957l1.818.78a3 3 0 002.364 0l5.508-2.361a11.026 11.026 0 01.25 3.762 1 1 0 01-.89.89 8.968 8.968 0 00-5.35 2.524 1 1 0 01-1.4 0zM6 18a1 1 0 001-1v-2.065a8.935 8.935 0 00-2-.712V17a1 1 0 001 1z"/>
              </svg>
            </div>
            <div>
              <div className="text-lg font-bold tracking-wide">Mathematics Quiz</div>
              <div className="text-xs opacity-90">November 21, 2025</div>
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
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-lg text-gray-600 font-medium">Loading exam...</p>
        </div>
      </div>
    );
  }

  if (error || !examData || !currentQuestion) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="w-14 h-14 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-7 h-7 text-red-500" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">Error Loading Exam</h2>
          <p className="text-gray-600 mb-6">{error || 'No question available'}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-6 py-3 bg-blue-500 text-white rounded-lg font-medium hover:bg-blue-600"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 pb-24">
      <Timer 
        durationMinutes={examData.template.durationMinutes} 
        questionTime={questionTime}
        onTimeUp={handleTimeUp} 
      />

      {/* Progress Bar */}
      <div className="w-full bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-2">
          <div className="flex items-center justify-between text-sm text-gray-600 mb-2">
            <span className="font-medium">Progress: {stats.answeredCount} of {examData.template.totalQuestions}</span>
            {stats.answeredCount > 0 && (
              <span className="font-semibold text-indigo-600">
                Accuracy: {stats.currentAccuracy.toFixed(0)}% 
                <span className="text-gray-500 ml-2">({stats.correctCount}✓ / {stats.wrongCount}✗)</span>
              </span>
            )}
          </div>
          <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
            <motion.div 
              className="h-full bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500 rounded-full"
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
              <div className="w-20 h-20 bg-gradient-to-br from-indigo-500 to-purple-600 text-white rounded-2xl flex items-center justify-center text-3xl font-bold shadow-lg">
                {stats.answeredCount + 1}
              </div>
              <div className="absolute -top-2 -right-2 w-7 h-7 bg-yellow-400 rounded-full flex items-center justify-center text-xs font-bold shadow-md">
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
              <svg className="w-6 h-6" fill={markedForReview ? "currentColor" : "none"} stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
              </svg>
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
                className="bg-white rounded-2xl shadow-lg p-8 border-2 border-indigo-100"
              >
                {/* Question Header */}
                <div className="flex items-center justify-between mb-6">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center">
                      <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-3a1 1 0 00-.867.5 1 1 0 11-1.731-1A3 3 0 0113 8a3.001 3.001 0 01-2 2.83V11a1 1 0 11-2 0v-1a1 1 0 011-1 1 1 0 100-2zm0 8a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
                      </svg>
                    </div>
                    <div>
                      <div className="text-xs text-gray-500 font-medium">Question {stats.answeredCount + 1}</div>
                      <div className="text-sm font-semibold text-gray-700">{currentQuestion.subject}</div>
                    </div>
                  </div>
                  {currentQuestion.topic && (
                    <span className="px-3 py-1 bg-purple-100 text-purple-700 rounded-full text-xs font-medium">
                      {currentQuestion.topic}
                    </span>
                  )}
                </div>

                {/* Question Text */}
                <div className="mb-8 p-6 bg-gradient-to-br from-indigo-50 to-purple-50 rounded-xl border-l-4 border-indigo-500">
                  <h2 className="text-2xl font-semibold text-gray-800 leading-relaxed">
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
                            ? 'border-indigo-500 bg-gradient-to-r from-indigo-50 to-purple-50 shadow-md'
                            : 'border-gray-200 bg-white hover:border-indigo-300 hover:shadow-sm'
                        }`}
                      >
                        <div className={`flex-shrink-0 w-12 h-12 rounded-xl flex items-center justify-center font-bold text-lg transition-all ${
                          isSelected
                            ? 'bg-gradient-to-br from-indigo-500 to-purple-600 text-white shadow-md scale-110'
                            : 'bg-gray-100 text-gray-600'
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
                    className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg"
                  >
                    <p className="text-sm text-blue-700 flex items-center gap-2">
                      <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                      </svg>
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
      <div className="fixed bottom-0 left-0 right-0 bg-white border-t-2 border-gray-200 shadow-2xl backdrop-blur-sm">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3 text-gray-600">
              <svg className="w-5 h-5 text-indigo-500" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z"/>
                <path fillRule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clipRule="evenodd"/>
              </svg>
              <span className="font-semibold text-lg">{examData.template.name}</span>
            </div>
            
            <motion.button
              onClick={handleSubmitAnswer}
              disabled={!selectedOptionId || submitting}
              whileHover={{ scale: selectedOptionId && !submitting ? 1.05 : 1 }}
              whileTap={{ scale: selectedOptionId && !submitting ? 0.95 : 1 }}
              className={`px-10 py-4 rounded-xl font-bold text-lg flex items-center gap-3 transition-all duration-300 ${
                selectedOptionId && !submitting
                  ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-lg hover:shadow-xl'
                  : 'bg-gray-200 text-gray-400 cursor-not-allowed'
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

            <div className="text-sm text-gray-500">
              Question <span className="font-bold text-indigo-600">{stats.answeredCount + 1}</span> of {examData.template.totalQuestions}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ExamPage;
