import React, { useEffect, useState } from "react";
import { getAnalytics, ApiException } from "./api";
import { useToast } from "./hooks/useToast";
import { motion } from "framer-motion";

interface AnalyticsData {
  chatCount: number;
  userCount: number;
  [key: string]: number | string;
}

const StatCard: React.FC<{
  icon: string;
  label: string;
  value: number | string;
  color: string;
  delay?: number;
}> = ({ icon, label, value, color, delay = 0 }) => (
  <motion.div
    className={`relative overflow-hidden rounded-2xl p-6 ${color}`}
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.4, delay }}
    whileHover={{ scale: 1.02, y: -4 }}
  >
    <div className="absolute top-0 right-0 -mt-4 -mr-4 w-24 h-24 bg-white/10 rounded-full blur-2xl" />
    <div className="relative z-10">
      <div className="flex items-center justify-between mb-4">
        <div className="w-12 h-12 rounded-xl bg-white/20 backdrop-blur-sm flex items-center justify-center">
          <span className="text-2xl">{icon}</span>
        </div>
      </div>
      <p className="text-white/80 text-sm font-medium mb-1">{label}</p>
      <p className="text-3xl font-bold text-white">{value}</p>
    </div>
  </motion.div>
);

const Analytics: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [data, setData] = useState<AnalyticsData | null>(null);
  const [loading, setLoading] = useState(false);
  const schoolId = "1";

  useEffect(() => {
    const fetchAnalytics = async () => {
      setLoading(true);
      try {
        const analyticsData = await getAnalytics(schoolId, token);
        setData(analyticsData as AnalyticsData);
      } catch (err) {
        const error = err as ApiException;
        toast.error(error.message || "Couldn't load your dashboard right now");
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    fetchAnalytics();
  }, [schoolId, token, toast]);

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 py-6">
      {/* Header */}
      <motion.div 
        className="text-center mb-8"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
      >
        <div className="page-header-icon mx-auto">
          <span className="text-2xl">📊</span>
        </div>
        <h2 className="page-header-title text-gradient">Learning Dashboard</h2>
        <p className="page-header-subtitle">Track how students are using Smart Study</p>
        <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-emerald-50 text-emerald-700 text-sm font-medium mt-3">
          <span className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse" />
          Live Data
        </div>
      </motion.div>

      {loading ? (
        <motion.div 
          className="card p-12 text-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          <div className="relative inline-flex">
            <div className="w-16 h-16 border-4 border-slate-200 rounded-full" />
            <div className="w-16 h-16 border-4 border-cyan-500 border-t-transparent rounded-full absolute top-0 left-0 animate-spin" />
          </div>
          <p className="text-slate-500 mt-4 font-medium">Loading your dashboard...</p>
        </motion.div>
      ) : !data ? (
        <motion.div 
          className="empty-state"
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
        >
          <div className="empty-state-icon">📈</div>
          <h3 className="empty-state-title">No Data Yet</h3>
          <p className="empty-state-text">
            Dashboard stats will appear here once students start using the platform.
            Share Smart Study with your class to get started!
          </p>
        </motion.div>
      ) : (
        <>
          {/* Stats Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            <StatCard
              icon="💬"
              label="Questions Asked"
              value={data.chatCount.toLocaleString()}
              color="bg-gradient-to-br from-cyan-500 to-teal-600"
              delay={0.1}
            />
            <StatCard
              icon="🧑‍🎓"
              label="Active Students"
              value={data.userCount.toLocaleString()}
              color="bg-gradient-to-br from-emerald-500 to-green-600"
              delay={0.2}
            />
            <StatCard
              icon="⭐"
              label="Questions per Student"
              value={data.userCount > 0 ? (data.chatCount / data.userCount).toFixed(1) : '0'}
              color="bg-gradient-to-br from-amber-500 to-orange-500"
              delay={0.3}
            />
          </div>

          {/* Platform Overview */}
          <motion.div
            className="card p-6 sm:p-8"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.4 }}
          >
            <h3 className="text-lg font-semibold text-slate-800 mb-6 flex items-center gap-2">
              <span>🎯</span>
              Platform Overview
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
              <div className="p-4 rounded-xl bg-gradient-to-br from-slate-50 to-slate-100 border border-slate-200/50">
                <p className="text-2xl font-bold text-slate-800">{data.chatCount}</p>
                <p className="text-sm text-slate-500">Total Questions</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-slate-50 to-slate-100 border border-slate-200/50">
                <p className="text-2xl font-bold text-slate-800">{data.userCount}</p>
                <p className="text-sm text-slate-500">Students</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-emerald-50 to-green-100 border border-emerald-200/50">
                <div className="flex items-center gap-2">
                  <span className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse" />
                  <p className="text-lg font-bold text-emerald-600">Online</p>
                </div>
                <p className="text-sm text-slate-500">System Status</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-cyan-50 to-teal-100 border border-cyan-200/50">
                <p className="text-lg font-bold text-cyan-600">🔒 Exam Safe</p>
                <p className="text-sm text-slate-500">Syllabus Only</p>
              </div>
            </div>
          </motion.div>

          {/* Info Box */}
          <motion.div
            className="info-box mt-6"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.5 }}
          >
            <h4 className="text-sm font-semibold text-cyan-800 mb-2 flex items-center gap-2">
              <span>💡</span>
              Understanding Your Dashboard
            </h4>
            <p className="text-sm text-cyan-700">
              This dashboard shows real-time usage of Smart Study in your school. 
              All student questions are answered strictly from your uploaded syllabus materials, 
              ensuring exam-safe learning.
            </p>
          </motion.div>
        </>
      )}
    </div>
  );
};

export default Analytics;
