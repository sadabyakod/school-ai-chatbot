import React, { useState } from 'react';
import { motion } from 'framer-motion';
import FileUpload from '../FileUpload';
import Faqs from '../Faqs';
import Analytics from '../Analytics';
import ExamHub from './ExamHub';
import { useToast } from '../hooks/useToast';

interface TeacherDashboardProps {
  onLogout: () => void;
}

const TeacherDashboard: React.FC<TeacherDashboardProps> = ({ onLogout }) => {
  const [activeTab, setActiveTab] = useState<'dashboard' | 'materials' | 'exams' | 'analytics' | 'faqs'>('dashboard');
  const toast = useToast();
  const token = localStorage.getItem('jwt') || '';

  const tabs = [
    { id: 'dashboard' as const, name: 'Dashboard', icon: '📊' },
    { id: 'materials' as const, name: 'Upload Materials', icon: '📤' },
    { id: 'exams' as const, name: 'Manage Exams', icon: '📝' },
    { id: 'analytics' as const, name: 'Student Analytics', icon: '📈' },
    { id: 'faqs' as const, name: 'FAQs', icon: '❓' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navigation */}
      <nav className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-purple-600 to-pink-600 rounded-xl flex items-center justify-center">
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                </svg>
              </div>
              <div>
                <h1 className="text-lg font-bold text-gray-900">SmartStudy AI</h1>
                <p className="text-xs text-gray-500">Teacher Portal</p>
              </div>
            </div>

            {/* Right Actions */}
            <div className="flex items-center gap-4">
              <div className="hidden sm:flex items-center gap-2 bg-purple-50 px-4 py-2 rounded-lg">
                <span className="text-2xl">👥</span>
                <div>
                  <p className="text-xs text-gray-600">Active Students</p>
                  <p className="text-sm font-bold text-purple-600">142</p>
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
          <div className="flex gap-1 py-2 overflow-x-auto">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-6 py-3 rounded-lg font-medium transition-all whitespace-nowrap ${
                  activeTab === tab.id
                    ? 'bg-purple-600 text-white shadow-lg'
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
        {activeTab === 'dashboard' && (
          <motion.div
            key="dashboard"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Welcome Back, Teacher!</h2>
              <p className="text-gray-600">Here's what's happening with your classes today</p>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-blue-600">142</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Total Students</h3>
                <p className="text-xs text-green-600">↑ 12 new this month</p>
              </div>

              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-purple-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-purple-600">24</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Active Exams</h3>
                <p className="text-xs text-gray-500">8 due this week</p>
              </div>

              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-green-600">89%</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">Pass Rate</h3>
                <p className="text-xs text-green-600">↑ 3% this semester</p>
              </div>

              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="w-12 h-12 bg-orange-100 rounded-xl flex items-center justify-center">
                    <svg className="w-6 h-6 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
                    </svg>
                  </div>
                  <span className="text-3xl font-bold text-orange-600">346</span>
                </div>
                <h3 className="text-sm font-medium text-gray-600 mb-1">AI Queries</h3>
                <p className="text-xs text-gray-500">Last 24 hours</p>
              </div>
            </div>

            {/* Recent Activity & Quick Actions */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Recent Activity */}
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h3 className="text-lg font-bold text-gray-900 mb-4">Recent Activity</h3>
                <div className="space-y-4">
                  {[
                    { student: 'Sarah Johnson', action: 'Completed Math Exam', score: 92, time: '5 min ago' },
                    { student: 'Mike Chen', action: 'Submitted Science Assignment', score: 88, time: '12 min ago' },
                    { student: 'Emma Davis', action: 'Asked question via AI', score: null, time: '18 min ago' },
                    { student: 'Alex Kumar', action: 'Completed History Exam', score: 95, time: '25 min ago' },
                  ].map((activity, idx) => (
                    <div key={idx} className="flex items-center justify-between p-3 bg-gray-50 rounded-xl">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-gradient-to-br from-purple-500 to-pink-500 rounded-full flex items-center justify-center text-white font-bold">
                          {activity.student.charAt(0)}
                        </div>
                        <div>
                          <h4 className="font-semibold text-gray-900 text-sm">{activity.student}</h4>
                          <p className="text-xs text-gray-500">{activity.action}</p>
                        </div>
                      </div>
                      <div className="text-right">
                        {activity.score && (
                          <div className="text-lg font-bold text-green-600">{activity.score}%</div>
                        )}
                        <p className="text-xs text-gray-400">{activity.time}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Quick Actions */}
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h3 className="text-lg font-bold text-gray-900 mb-4">Quick Actions</h3>
                <div className="grid grid-cols-2 gap-4">
                  <button
                    onClick={() => setActiveTab('materials')}
                    className="p-4 bg-gradient-to-br from-blue-50 to-blue-100 hover:from-blue-100 hover:to-blue-200 rounded-xl transition-all group"
                  >
                    <div className="w-12 h-12 bg-blue-600 rounded-xl flex items-center justify-center mb-3 group-hover:scale-110 transition-transform">
                      <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                      </svg>
                    </div>
                    <h4 className="font-semibold text-gray-900 text-sm">Upload Materials</h4>
                  </button>

                  <button
                    onClick={() => setActiveTab('exams')}
                    className="p-4 bg-gradient-to-br from-purple-50 to-purple-100 hover:from-purple-100 hover:to-purple-200 rounded-xl transition-all group"
                  >
                    <div className="w-12 h-12 bg-purple-600 rounded-xl flex items-center justify-center mb-3 group-hover:scale-110 transition-transform">
                      <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                      </svg>
                    </div>
                    <h4 className="font-semibold text-gray-900 text-sm">Create Exam</h4>
                  </button>

                  <button
                    onClick={() => setActiveTab('analytics')}
                    className="p-4 bg-gradient-to-br from-green-50 to-green-100 hover:from-green-100 hover:to-green-200 rounded-xl transition-all group"
                  >
                    <div className="w-12 h-12 bg-green-600 rounded-xl flex items-center justify-center mb-3 group-hover:scale-110 transition-transform">
                      <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                      </svg>
                    </div>
                    <h4 className="font-semibold text-gray-900 text-sm">View Analytics</h4>
                  </button>

                  <button
                    onClick={() => setActiveTab('faqs')}
                    className="p-4 bg-gradient-to-br from-orange-50 to-orange-100 hover:from-orange-100 hover:to-orange-200 rounded-xl transition-all group"
                  >
                    <div className="w-12 h-12 bg-orange-600 rounded-xl flex items-center justify-center mb-3 group-hover:scale-110 transition-transform">
                      <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                    </div>
                    <h4 className="font-semibold text-gray-900 text-sm">Manage FAQs</h4>
                  </button>
                </div>
              </div>
            </div>
          </motion.div>
        )}

        {activeTab === 'materials' && (
          <motion.div
            key="materials"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Upload Study Materials</h2>
              <p className="text-gray-600">Upload documents that the AI will use to answer student questions</p>
            </div>
            <FileUpload token={token} toast={toast} />
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
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Manage Exams</h2>
              <p className="text-gray-600">Create and manage exam templates for your students</p>
            </div>
            <ExamHub token={token} toast={toast} />
          </motion.div>
        )}

        {activeTab === 'analytics' && (
          <motion.div
            key="analytics"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Student Analytics</h2>
              <p className="text-gray-600">Monitor student performance and engagement</p>
            </div>
            <Analytics token={token} toast={toast} />
          </motion.div>
        )}

        {activeTab === 'faqs' && (
          <motion.div
            key="faqs"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Manage FAQs</h2>
              <p className="text-gray-600">Create and update frequently asked questions</p>
            </div>
            <Faqs token={token} toast={toast} />
          </motion.div>
        )}
      </main>
    </div>
  );
};

export default TeacherDashboard;
