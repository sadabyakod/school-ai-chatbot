import React, { useEffect, useState } from "react";
import { API_URL } from "./api";


const Faqs: React.FC<{ token?: string }> = ({ token }) => {
  const [faqs, setFaqs] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  // TODO: Replace with real schoolId if needed
  const schoolId = 1;

  useEffect(() => {
    const fetchFaqs = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${API_URL}/faqs`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!res.ok) throw new Error("Failed to fetch FAQs");
        setFaqs(await res.json());
      } catch {
        setFaqs([]);
      } finally {
        setLoading(false);
      }
    };
    fetchFaqs();
  }, [schoolId]);

  return (
    <div className="max-w-md mx-auto mt-8 bg-white p-6 rounded shadow">
      <h2 className="text-lg font-bold mb-4">School FAQs</h2>
      {loading ? (
        <div>Loading...</div>
      ) : faqs.length === 0 ? (
        <div>No FAQs found.</div>
      ) : (
        <ul className="space-y-2">
          {faqs.map((faq) => (
            <li key={faq.id}>
              <div className="font-semibold">Q: {faq.question}</div>
              <div className="ml-2">A: {faq.answer}</div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default Faqs;
