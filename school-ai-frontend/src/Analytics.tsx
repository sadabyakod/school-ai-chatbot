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
  icon: React.ReactNode;
  label: string;
  value: number | string;
  color: string;
  delay?: number;
}> = ({ icon, label, value, color, delay = 0 }) => (
  <motion.div
    className={`relative overflow-hidden rounded-2xl p-6 ${color}`}
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.5, delay }}
    whileHover={{ scale: 1.02, y: -5 }}
  >
    <div className="absolute top-0 right-0 -mt-4 -mr-4 w-24 h-24 bg-white/10 rounded-full blur-2xl" />
    <div className="relative z-10">
      <div className="flex items-center justify-between mb-4">
        <div className="w-12 h-12 rounded-xl bg-white/20 backdrop-blur-sm flex items-center justify-center">
          {icon}
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
  const schoolId = "1"; // TODO: Replace with real schoolId if needed

  useEffect(() => {
    const fetchAnalytics = async () => {
      setLoading(true);
      try {
        const analyticsData = await getAnalytics(schoolId, token);
        setData(analyticsData as AnalyticsData);
      } catch (err) {
        const error = err as ApiException;
        toast.error(error.message || "Failed to load analytics");
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    fetchAnalytics();
  }, [schoolId, token, toast]);

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 py-6 sm:py-8">
      {/* Header */}
      <motion.div 
        className="text-center mb-8"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-gradient-to-r from-violet-100 to-purple-100 text-purple-700 text-sm font-semibold mb-4">
          <span className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
          Live Analytics
        </div>
        <h2 className="text-2xl sm:text-3xl font-bold text-gradient mb-2">School Dashboard</h2>
        <p className="text-gray-500">Real-time insights into platform usage</p>
      </motion.div>

      {loading ? (
        <motion.div 
          className="glass rounded-3xl p-12 text-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          <div className="relative inline-flex">
            <div className="w-16 h-16 border-4 border-purple-200 rounded-full" />
            <div className="w-16 h-16 border-4 border-purple-500 border-t-transparent rounded-full absolute top-0 left-0 animate-spin" />
          </div>
          <p className="text-gray-500 mt-4 font-medium">Loading analytics...</p>
        </motion.div>
      ) : !data ? (
        <motion.div 
          className="glass rounded-3xl p-12 text-center"
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
        >
          <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gray-100 flex items-center justify-center">
            <svg className="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">No Analytics Available</h3>
          <p className="text-gray-500">Analytics data will appear here once available.</p>
        </motion.div>
      ) : (
        <>
          {/* Stats Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            <StatCard
              icon={
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                </svg>
              }
              label="Total Chats"
              value={data.chatCount.toLocaleString()}
              color="bg-gradient-to-br from-violet-500 to-purple-600"
              delay={0.1}
            />
            <StatCard
              icon={
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
              }
              label="Active Users"
              value={data.userCount.toLocaleString()}
              color="bg-gradient-to-br from-fuchsia-500 to-pink-600"
              delay={0.2}
            />
            <StatCard
              icon={
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              }
              label="Avg. Chats/User"
              value={data.userCount > 0 ? (data.chatCount / data.userCount).toFixed(1) : '0'}
              color="bg-gradient-to-br from-amber-500 to-orange-600"
              delay={0.3}
            />
          </div>

          {/* Quick Stats Card */}
          <motion.div
            className="glass rounded-3xl p-6 sm:p-8"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, delay: 0.4 }}
          >
            <h3 className="text-lg font-semibold text-gray-800 mb-6 flex items-center gap-2">
              <svg className="w-5 h-5 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
              Platform Overview
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
              <div className="p-4 rounded-xl bg-gradient-to-br from-gray-50 to-gray-100 border border-gray-200/50">
                <p className="text-2xl font-bold text-gray-800">{data.chatCount}</p>
                <p className="text-sm text-gray-500">Messages</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-gray-50 to-gray-100 border border-gray-200/50">
                <p className="text-2xl font-bold text-gray-800">{data.userCount}</p>
                <p className="text-sm text-gray-500">Users</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-green-50 to-emerald-100 border border-green-200/50">
                <p className="text-2xl font-bold text-green-600">Active</p>
                <p className="text-sm text-gray-500">Status</p>
              </div>
              <div className="p-4 rounded-xl bg-gradient-to-br from-purple-50 to-violet-100 border border-purple-200/50">
                <p className="text-2xl font-bold text-purple-600">AI</p>
                <p className="text-sm text-gray-500">Powered</p>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </div>
  );
};

export default Analytics;
