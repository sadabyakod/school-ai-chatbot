import React, { useEffect, useState } from "react";
import { API_URL } from "./api";


const Analytics: React.FC<{ token?: string }> = ({ token }) => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  // TODO: Replace with real schoolId if needed
  const schoolId = 1;

  useEffect(() => {
    const fetchAnalytics = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${API_URL}/analytics?schoolId=${schoolId}`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!res.ok) throw new Error("Failed to fetch analytics");
        setData(await res.json());
      } catch {
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    fetchAnalytics();
  }, [schoolId]);

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
