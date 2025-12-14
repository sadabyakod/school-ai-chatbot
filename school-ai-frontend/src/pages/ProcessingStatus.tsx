import React, { useState, useEffect, useCallback } from "react";
import { motion } from "framer-motion";
import { useToast } from "../hooks/useToast";
import { pollSubmissionStatus, getExamResults, ApiException } from "../api";
import type { SubmissionStatusResponse, WrittenExamResult } from "../api";

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface ProcessingStatusProps {
  writtenSubmissionId: string;
  examId: string;
  studentId: string;
  onComplete: (result: WrittenExamResult) => void;
  onBack: () => void;
  toast: ReturnType<typeof useToast>;
}

type ProcessingStage = 'PendingEvaluation' | 'OcrProcessing' | 'Evaluating' | 'Completed' | 'Failed';

interface StageInfo {
  emoji: string;
  title: string;
  description: string;
  color: string;
}

const STAGE_INFO: Record<ProcessingStage, StageInfo> = {
  PendingEvaluation: {
    emoji: '⏳',
    title: 'Queued for Processing',
    description: 'Your answer sheet is in the queue and will be processed shortly...',
    color: 'from-amber-400 to-orange-500',
  },
  OcrProcessing: {
    emoji: '📄',
    title: 'Extracting Text',
    description: 'AI is reading your handwritten answers using advanced OCR...',
    color: 'from-cyan-400 to-teal-500',
  },
  Evaluating: {
    emoji: '🤖',
    title: 'AI Evaluation in Progress',
    description: 'Our AI is carefully analyzing and grading your answers...',
    color: 'from-teal-400 to-emerald-500',
  },
  Completed: {
    emoji: '✅',
    title: 'Evaluation Complete!',
    description: 'Your results are ready. Loading your scores...',
    color: 'from-emerald-400 to-green-500',
  },
  Failed: {
    emoji: '❌',
    title: 'Evaluation Failed',
    description: 'Something went wrong. Please try uploading again or contact support.',
    color: 'from-red-400 to-rose-500',
  },
};

const POLLING_INTERVAL_MS = 3000; // 3 seconds as recommended

// ==========================================
// STAGE INDICATOR COMPONENT
// ==========================================

const StageIndicator: React.FC<{ 
  stage: ProcessingStage; 
  currentStage: ProcessingStage;
  index: number;
}> = ({ stage, currentStage, index }) => {
  const stages: ProcessingStage[] = ['PendingEvaluation', 'OcrProcessing', 'Evaluating', 'Completed'];
  const currentIndex = stages.indexOf(currentStage);
  const stageIndex = stages.indexOf(stage);
  
  const isActive = currentStage === stage;
  const isComplete = stageIndex < currentIndex || currentStage === 'Completed';
  const isFailed = currentStage === 'Failed';

  return (
    <motion.div
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ delay: index * 0.1 }}
      className={`flex items-center gap-4 p-4 rounded-xl transition-all duration-500 ${
        isActive && !isFailed
          ? 'bg-gradient-to-r from-cyan-50 to-teal-50 border-2 border-cyan-300 shadow-md scale-105'
          : isComplete
          ? 'bg-emerald-50 border-2 border-emerald-200'
          : 'bg-slate-50 border-2 border-slate-100 opacity-50'
      }`}
    >
      <div className={`w-12 h-12 rounded-xl flex items-center justify-center text-2xl ${
        isActive && !isFailed
          ? 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg'
          : isComplete
          ? 'bg-gradient-to-br from-emerald-400 to-green-500'
          : 'bg-slate-200'
      }`}>
        {isComplete ? '✓' : STAGE_INFO[stage].emoji}
      </div>
      <div className="flex-1">
        <h4 className={`font-semibold ${
          isActive ? 'text-teal-700' : isComplete ? 'text-emerald-700' : 'text-slate-400'
        }`}>
          {STAGE_INFO[stage].title}
        </h4>
        {isActive && !isFailed && (
          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="text-sm text-slate-600"
          >
            {STAGE_INFO[stage].description}
          </motion.p>
        )}
      </div>
      {isActive && !isFailed && (
        <div className="w-6 h-6 border-3 border-cyan-500 border-t-transparent rounded-full animate-spin"></div>
      )}
    </motion.div>
  );
};

// ==========================================
// MAIN COMPONENT
// ==========================================

const ProcessingStatus: React.FC<ProcessingStatusProps> = ({
  writtenSubmissionId,
  examId,
  studentId,
  onComplete,
  onBack,
  toast,
}) => {
  const [status, setStatus] = useState<SubmissionStatusResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pollCount, setPollCount] = useState(0);

  const currentStage = (status?.status || 'PendingEvaluation') as ProcessingStage;
  const stageInfo = STAGE_INFO[currentStage];

  // Fetch results when complete
  const fetchResults = useCallback(async () => {
    try {
      const result = await getExamResults(examId, studentId);
      toast.showToast('🎉 Results loaded successfully!', 'success');
      onComplete(result);
    } catch (err) {
      const error = err as ApiException;
      toast.showToast('Failed to load results: ' + error.message, 'error');
      setError('Failed to load results. Please try again.');
    }
  }, [examId, studentId, onComplete, toast]);

  // Polling effect
  useEffect(() => {
    let intervalId: ReturnType<typeof setInterval>;
    let mounted = true;

    const poll = async () => {
      try {
        const response = await pollSubmissionStatus(writtenSubmissionId);
        
        if (!mounted) return;
        
        setStatus(response);
        setPollCount(prev => prev + 1);

        if (response.isComplete) {
          // Clear interval and fetch results
          clearInterval(intervalId);
          
          if (response.status === 'Completed') {
            await fetchResults();
          } else if (response.status === 'Failed') {
            setError('Evaluation failed. Please try uploading again.');
          }
        }
      } catch (err) {
        const error = err as ApiException;
        console.error('Polling error:', error);
        
        // Don't stop polling on transient errors, but show warning after 3 failures
        if (pollCount > 3) {
          setError('Having trouble connecting to the server. Will keep trying...');
        }
      }
    };

    // Initial poll
    poll();
    
    // Start polling interval
    intervalId = setInterval(poll, POLLING_INTERVAL_MS);

    return () => {
      mounted = false;
      clearInterval(intervalId);
    };
  }, [writtenSubmissionId, pollCount, fetchResults]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 py-8 px-4">
      <div className="max-w-xl mx-auto">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-8"
        >
          <motion.div
            key={currentStage}
            initial={{ scale: 0.5, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ type: "spring", stiffness: 200 }}
            className={`inline-flex items-center justify-center w-24 h-24 bg-gradient-to-br ${stageInfo.color} rounded-3xl shadow-2xl mb-6`}
          >
            <span className="text-5xl">{stageInfo.emoji}</span>
          </motion.div>
          
          <motion.h1
            key={stageInfo.title}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-3xl font-bold text-slate-800 mb-2"
          >
            {stageInfo.title}
          </motion.h1>
          
          <motion.p
            key={stageInfo.description}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="text-lg text-slate-600"
          >
            {status?.statusMessage || stageInfo.description}
          </motion.p>
        </motion.div>

        {/* Stage Progress */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white rounded-2xl shadow-xl border border-gray-100 p-6 mb-6"
        >
          <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-4">
            Processing Stages
          </h3>
          
          <div className="space-y-3">
            {(['PendingEvaluation', 'OcrProcessing', 'Evaluating', 'Completed'] as ProcessingStage[]).map((stage, index) => (
              <StageIndicator
                key={stage}
                stage={stage}
                currentStage={currentStage}
                index={index}
              />
            ))}
          </div>
        </motion.div>

        {/* Submission Info */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white rounded-2xl shadow-xl border border-gray-100 p-6 mb-6"
        >
          <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-4">
            Submission Details
          </h3>
          
          <div className="grid grid-cols-2 gap-4">
            <div className="p-3 bg-slate-50 rounded-xl">
              <div className="text-xs text-slate-500 mb-1">Submission ID</div>
              <div className="font-mono text-sm font-semibold text-teal-600 truncate">
                {writtenSubmissionId}
              </div>
            </div>
            <div className="p-3 bg-gray-50 rounded-xl">
              <div className="text-xs text-gray-500 mb-1">Exam ID</div>
              <div className="font-mono text-sm font-semibold text-gray-700 truncate">
                {examId}
              </div>
            </div>
            <div className="p-3 bg-gray-50 rounded-xl">
              <div className="text-xs text-gray-500 mb-1">Student ID</div>
              <div className="font-mono text-sm font-semibold text-gray-700 truncate">
                {studentId}
              </div>
            </div>
            <div className="p-3 bg-gray-50 rounded-xl">
              <div className="text-xs text-gray-500 mb-1">Checks</div>
              <div className="text-sm font-semibold text-gray-700">
                {pollCount} poll{pollCount !== 1 ? 's' : ''}
              </div>
            </div>
          </div>
        </motion.div>

        {/* Error Message */}
        {error && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="bg-red-50 border-2 border-red-200 rounded-xl p-4 mb-6"
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <svg className="w-5 h-5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <p className="text-red-700 font-medium">{error}</p>
            </div>
          </motion.div>
        )}

        {/* Back Button */}
        {(currentStage === 'Failed' || error) && (
          <motion.button
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            onClick={onBack}
            className="w-full py-4 bg-gradient-to-r from-gray-100 to-gray-200 text-gray-700 rounded-xl font-bold hover:from-gray-200 hover:to-gray-300 transition-all"
          >
            ← Back to Upload
          </motion.button>
        )}

        {/* Helpful Tips */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.5 }}
          className="mt-6 p-4 bg-gradient-to-br from-cyan-50 to-teal-50 rounded-xl border border-cyan-100"
        >
          <div className="flex items-start gap-3">
            <span className="text-xl">💡</span>
            <div className="text-sm text-cyan-800">
              <p className="font-semibold mb-1">Please wait...</p>
              <p className="text-cyan-700">
                Evaluation typically takes 1-3 minutes depending on the number of pages.
                You can leave this page open - we'll show your results automatically!
              </p>
            </div>
          </div>
        </motion.div>
      </div>
    </div>
  );
};

export default ProcessingStatus;
