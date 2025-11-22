import React, { useState } from 'react';
import { motion } from 'framer-motion';
import ChatBot from '../ChatBot';
import ExamHub from './ExamHub';
import { useToast } from '../hooks/useToast';

interface StudentDashboardProps {
  onLogout: () => void;
}

const StudentDashboard: React.FC<StudentDashboardProps> = ({ onLogout }) => {
  const [activeTab, setActiveTab] = useState<'chat' | 'exams' | 'progress'>('chat');
  const toast = useToast();
  const token = localStorage.getItem('jwt') || '';

  const tabs = [
    { id: 'chat' as const, name: 'Ask AI', icon: '💬' },
    { id: 'exams' as const, name: 'Practice Exams', icon: '📝' },
    { id: 'progress' as const, name: 'My Progress', icon: '📊' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navigation */}
      <nav className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-cyan-600 rounded-xl flex items-center justify-center">
                <svg className="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3z"/>
                </svg>
              </div>
              <div>
                <h1 className="text-lg font-bold text-gray-900">SmartStudy AI</h1>
                <p className="text-xs text-gray-500">Student Portal</p>
              </div>
            </div>

            {/* Right Actions */}
            <div className="flex items-center gap-4">
              <div className="hidden sm:flex items-center gap-2 bg-blue-50 px-4 py-2 rounded-lg">
                <span className="text-2xl">🎯</span>
                <div>
                  <p className="text-xs text-gray-600">Current Streak</p>
                  <p className="text-sm font-bold text-blue-600">5 days</p>
                </div>
              </div>
              <button
                onClick={onLogout}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                <span className="hidden sm:inline">Logout</span>
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Tab Navigation */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex gap-1 py-2">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-6 py-3 rounded-lg font-medium transition-all ${
                  activeTab === tab.id
                    ? 'bg-blue-600 text-white shadow-lg'
                    : 'text-gray-600 hover:bg-gray-100'
                }`}
              >
                <span className="text-xl">{tab.icon}</span>
                <span>{tab.name}</span>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {activeTab === 'chat' && (
          <motion.div
            key="chat"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Ask Your Study Questions</h2>
              <p className="text-gray-600">Get instant help from our AI tutor. Ask anything about your studies!</p>
            </div>
            <ChatBot token={token} toast={toast} />
          </motion.div>
        )}

        {activeTab === 'exams' && (
          <motion.div
            key="exams"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Practice Exams</h2>
              <p className="text-gray-600">Test your knowledge with adaptive practice exams</p>
            </div>
            <ExamHub token={token} toast={toast} />
          </motion.div>
        )}

        {activeTab === 'progress' && (
          <motion.div
            key="progress"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Your Progress</h2>
              <p className="text-gray-600">Track your learning journey and achievements</p>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-blue-600">12</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Exams Completed</h3>
                <p className="text-xs text-gray-500">+3 this week</p>
              </div>

              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-green-600">78%</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Average Score</h3>
                <p className="text-xs text-green-600">↑ 5% improvement</p>
              </div>

              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-purple-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-purple-600">24h</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Study Time</h3>
                <p className="text-xs text-gray-500">Last 7 days</p>
              </div>
            </div>

            {/* Recent Activity */}
            <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
              <h3 className="text-lg font-bold text-gray-900 mb-4">Recent Activity</h3>
              <div className="space-y-4">
                {[
                  { subject: 'Mathematics', score: 85, date: 'Today', status: 'passed' },
                  { subject: 'Science', score: 72, date: 'Yesterday', status: 'passed' },
                  { subject: 'History', score: 91, date: '2 days ago', status: 'excellent' },
                  { subject: 'English', score: 68, date: '3 days ago', status: 'passed' },
                ].map((activity, idx) => (
                  <div key={idx} className="flex items-center justify-between p-4 bg-gray-50 rounded-xl">
                    <div className="flex items-center gap-4">
                      <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                        activity.status === 'excellent' ? 'bg-green-100' : 'bg-blue-100'
                      }`}>
                        <span className="text-lg">{activity.status === 'excellent' ? '🏆' : '📚'}</span>
                      </div>
                      <div>
                        <h4 className="font-semibold text-gray-900">{activity.subject}</h4>
                        <p className="text-sm text-gray-500">{activity.date}</p>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className={`text-2xl font-bold ${
                        activity.score >= 85 ? 'text-green-600' : 'text-blue-600'
                      }`}>
                        {activity.score}%
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </motion.div>
        )}
      </main>
    </div>
  );
};

export default StudentDashboard;
