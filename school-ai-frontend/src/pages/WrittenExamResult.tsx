import React from "react";
import { motion } from "framer-motion";
import type { WrittenExamResult as ResultType } from "../api";

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface WrittenExamResultProps {
  result: ResultType;
  onBack: () => void;
}

// ==========================================
// GRADE BADGE COMPONENT
// ==========================================

const GradeBadge: React.FC<{ grade: string; passed: boolean }> = ({ grade, passed }) => {
  const getGradeColor = () => {
    if (!passed) return 'from-red-400 to-pink-500';
    switch (grade) {
      case 'A+': case 'A': return 'from-emerald-400 to-green-500';
      case 'B+': case 'B': return 'from-blue-400 to-indigo-500';
      case 'C+': case 'C': return 'from-yellow-400 to-orange-500';
      default: return 'from-orange-400 to-red-500';
    }
  };

  return (
    <motion.div
      initial={{ scale: 0, rotate: -180 }}
      animate={{ scale: 1, rotate: 0 }}
      transition={{ type: "spring", stiffness: 200, delay: 0.3 }}
      className={`w-24 h-24 rounded-3xl bg-gradient-to-br ${getGradeColor()} shadow-2xl flex items-center justify-center`}
    >
      <span className="text-4xl font-black text-white">{grade}</span>
    </motion.div>
  );
};

// ==========================================
// SCORE CARD COMPONENT
// ==========================================

const ScoreCard: React.FC<{
  title: string;
  score: number;
  total: number;
  icon: React.ReactNode;
  color: string;
  delay: number;
}> = ({ title, score, total, icon, color, delay }) => {
  const percentage = total > 0 ? (score / total) * 100 : 0;
  
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay }}
      className="bg-white rounded-2xl shadow-lg border border-gray-100 p-5"
    >
      <div className="flex items-center gap-3 mb-4">
        <div className={`w-10 h-10 bg-gradient-to-br ${color} rounded-xl flex items-center justify-center shadow-md`}>
          {icon}
        </div>
        <h3 className="font-semibold text-gray-700">{title}</h3>
      </div>
      
      <div className="mb-3">
        <div className="flex items-baseline gap-1">
          <span className="text-3xl font-bold text-gray-800">{score.toFixed(1)}</span>
          <span className="text-lg text-gray-400">/ {total}</span>
        </div>
        <span className={`text-sm font-semibold ${
          percentage >= 80 ? 'text-green-600' : percentage >= 60 ? 'text-blue-600' : percentage >= 40 ? 'text-orange-600' : 'text-red-600'
        }`}>
          {percentage.toFixed(0)}%
        </span>
      </div>
      
      <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ duration: 1, delay: delay + 0.3 }}
          className={`h-full bg-gradient-to-r ${color} rounded-full`}
        />
      </div>
    </motion.div>
  );
};

// ==========================================
// SUBJECTIVE RESULT CARD
// ==========================================

const SubjectiveResultCard: React.FC<{
  result: ResultType['subjectiveResults'][0];
  index: number;
}> = ({ result, index }) => {
  const percentage = result.maxMarks > 0 ? (result.earnedMarks / result.maxMarks) * 100 : 0;
  
  return (
    <motion.div
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ delay: 0.1 * index }}
      className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden"
    >
      {/* Header */}
      <div className={`px-5 py-4 ${
        percentage >= 80 ? 'bg-gradient-to-r from-green-50 to-emerald-50' :
        percentage >= 60 ? 'bg-gradient-to-r from-blue-50 to-indigo-50' :
        percentage >= 40 ? 'bg-gradient-to-r from-yellow-50 to-orange-50' :
        'bg-gradient-to-r from-red-50 to-pink-50'
      }`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold text-white ${
              percentage >= 80 ? 'bg-gradient-to-br from-green-500 to-emerald-600' :
              percentage >= 60 ? 'bg-gradient-to-br from-blue-500 to-indigo-600' :
              percentage >= 40 ? 'bg-gradient-to-br from-yellow-500 to-orange-600' :
              'bg-gradient-to-br from-red-500 to-pink-600'
            }`}>
              Q{index + 1}
            </div>
            <div>
              <div className="text-xs text-gray-500 font-medium">Question {result.questionId}</div>
              <div className="font-bold text-gray-800">
                {result.earnedMarks.toFixed(1)} / {result.maxMarks} marks
              </div>
            </div>
          </div>
          <div className={`px-3 py-1 rounded-full text-sm font-bold ${
            percentage >= 80 ? 'bg-green-200 text-green-800' :
            percentage >= 60 ? 'bg-blue-200 text-blue-800' :
            percentage >= 40 ? 'bg-orange-200 text-orange-800' :
            'bg-red-200 text-red-800'
          }`}>
            {percentage.toFixed(0)}%
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="p-5 space-y-4">
        {/* Expected Answer */}
        {result.expectedAnswer && (
          <div>
            <h4 className="text-sm font-semibold text-gray-500 mb-2 flex items-center gap-2">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              Expected Answer
            </h4>
            <p className="text-gray-700 bg-green-50 p-3 rounded-lg text-sm border border-green-100">
              {result.expectedAnswer}
            </p>
          </div>
        )}

        {/* Step Analysis */}
        {result.stepAnalysis && result.stepAnalysis.length > 0 && (
          <div>
            <h4 className="text-sm font-semibold text-gray-500 mb-2 flex items-center gap-2">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z" />
                <path fillRule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clipRule="evenodd" />
              </svg>
              Step-by-Step Analysis
            </h4>
            <div className="space-y-2">
              {result.stepAnalysis.map((step: any, i: number) => (
                <div key={i} className="text-sm p-2 bg-gray-50 rounded-lg border border-gray-100">
                  {typeof step === 'string' ? step : JSON.stringify(step)}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Feedback */}
        {result.overallFeedback && (
          <div>
            <h4 className="text-sm font-semibold text-gray-500 mb-2 flex items-center gap-2">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 13V5a2 2 0 00-2-2H4a2 2 0 00-2 2v8a2 2 0 002 2h3l3 3 3-3h3a2 2 0 002-2zM5 7a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1zm1 3a1 1 0 100 2h3a1 1 0 100-2H6z" clipRule="evenodd" />
              </svg>
              AI Feedback
            </h4>
            <p className="text-gray-700 bg-blue-50 p-3 rounded-lg text-sm border border-blue-100">
              {result.overallFeedback}
            </p>
          </div>
        )}
      </div>
    </motion.div>
  );
};

// ==========================================
// MAIN COMPONENT
// ==========================================

const WrittenExamResultView: React.FC<WrittenExamResultProps> = ({ result, onBack }) => {
  const hasMcq = result.mcqTotalMarks > 0;
  const hasSubjective = result.subjectiveTotalMarks > 0;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-cyan-50 to-teal-50 py-8 px-4">
      <div className="max-w-4xl mx-auto">
        {/* Back Button */}
        <motion.button
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          onClick={onBack}
          className="flex items-center gap-2 text-slate-600 hover:text-cyan-600 mb-6 transition-colors"
        >
          <span>←</span>
          <span className="font-medium">Back to Exams</span>
        </motion.button>

        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-8"
        >
          <GradeBadge grade={result.grade} passed={result.passed} />
          
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5 }}
            className="mt-6"
          >
            <h1 className={`text-4xl font-bold mb-2 ${
              result.passed 
                ? 'bg-gradient-to-r from-green-600 to-emerald-600 bg-clip-text text-transparent' 
                : 'bg-gradient-to-r from-red-600 to-pink-600 bg-clip-text text-transparent'
            }`}>
              {result.passed ? '🎉 Congratulations!' : 'Keep Trying!'}
            </h1>
            <p className="text-lg text-slate-600">
              You scored <span className="font-bold text-teal-600">{result.percentage.toFixed(1)}%</span> on this exam
            </p>
          </motion.div>
        </motion.div>

        {/* Grand Total Card */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.2 }}
          className={`bg-gradient-to-r ${
            result.passed 
              ? 'from-green-500 via-emerald-500 to-teal-500' 
              : 'from-orange-500 via-red-500 to-pink-500'
          } rounded-3xl shadow-2xl p-8 mb-8 text-white`}
        >
          <div className="text-center">
            <div className="text-sm font-medium uppercase tracking-wide opacity-80 mb-2">
              Total Score
            </div>
            <div className="flex items-baseline justify-center gap-2">
              <span className="text-6xl font-black">{result.grandScore.toFixed(1)}</span>
              <span className="text-3xl font-bold opacity-70">/ {result.grandTotalMarks}</span>
            </div>
          </div>
        </motion.div>

        {/* Score Cards Grid */}
        <div className="grid md:grid-cols-2 gap-6 mb-8">
          {hasMcq && (
            <ScoreCard
              title="MCQ Section"
              score={result.mcqScore}
              total={result.mcqTotalMarks}
              color="from-cyan-400 to-teal-500"
              delay={0.3}
              icon={
                <span className="text-lg">✅</span>
              }
            />
          )}
          {hasSubjective && (
            <ScoreCard
              title="Written Section"
              score={result.subjectiveScore}
              total={result.subjectiveTotalMarks}
              color="from-teal-400 to-emerald-500"
              delay={0.4}
              icon={
                <span className="text-lg">✍️</span>
              }
            />
          )}
        </div>

        {/* Subjective Results */}
        {hasSubjective && result.subjectiveResults && result.subjectiveResults.length > 0 && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.5 }}
          >
            <h2 className="text-2xl font-bold text-slate-800 mb-6 flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-cyan-500 to-teal-600 rounded-xl flex items-center justify-center shadow-md">
                <span className="text-xl">📝</span>
              </div>
              Detailed Question Analysis
            </h2>
            
            <div className="space-y-6">
              {result.subjectiveResults.map((subResult, index) => (
                <SubjectiveResultCard
                  key={subResult.questionId}
                  result={subResult}
                  index={index}
                />
              ))}
            </div>
          </motion.div>
        )}

        {/* Exam Info Footer */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.7 }}
          className="mt-8 p-4 bg-white rounded-xl border border-gray-100 shadow-sm"
        >
          <div className="flex flex-wrap gap-4 text-sm text-gray-500">
            <div>
              <span className="font-medium">Exam ID:</span> {result.examId}
            </div>
            <div>
              <span className="font-medium">Student ID:</span> {result.studentId}
            </div>
          </div>
        </motion.div>

        {/* Done Button */}
        <motion.button
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.8 }}
          onClick={onBack}
          whileHover={{ scale: 1.02 }}
          whileTap={{ scale: 0.98 }}
          className="w-full mt-8 py-4 bg-gradient-to-r from-cyan-500 via-teal-500 to-emerald-500 text-white rounded-xl font-bold text-lg shadow-lg hover:shadow-xl transition-all"
        >
          Done - Back to Exams
        </motion.button>
      </div>
    </div>
  );
};

export default WrittenExamResultView;
