import React, { useEffect, useState } from "react";
import { getFaqs, ApiException } from "./api";
import { useToast } from "./hooks/useToast";

const Faqs: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [faqs, setFaqs] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchFaqs = async () => {
      setLoading(true);
      try {
        const data = await getFaqs(token);
        setFaqs(data);
      } catch (err) {
        const error = err as ApiException;
        toast.error(error.message || "Failed to load FAQs");
        setFaqs([]);
      } finally {
        setLoading(false);
      }
    };
    fetchFaqs();
  }, [token, toast]);

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
