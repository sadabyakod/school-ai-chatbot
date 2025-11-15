import React, { useEffect, useState } from "react";
import { getAnalytics, ApiException } from "./api";
import { useToast } from "./hooks/useToast";

const Analytics: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const schoolId = "1"; // TODO: Replace with real schoolId if needed

  useEffect(() => {
    const fetchAnalytics = async () => {
      setLoading(true);
      try {
        const analyticsData = await getAnalytics(schoolId, token);
        setData(analyticsData);
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
    <div className="max-w-md mx-auto mt-8 bg-white p-6 rounded shadow">
      <h2 className="text-lg font-bold mb-4">School Analytics</h2>
      {loading ? (
        <div>Loading...</div>
      ) : !data ? (
        <div>No analytics found.</div>
      ) : (
        <ul className="space-y-2">
          <li>Chats: {data.chatCount}</li>
          <li>Users: {data.userCount}</li>
        </ul>
      )}
    </div>
  );
};

export default Analytics;
