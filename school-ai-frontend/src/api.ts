// API utility for backend calls
export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';

export async function sendChat({ userId, schoolId, message, language, token }: {
  userId: number;
  schoolId: number;
  message: string;
  language: string;
  token?: string;
}) {
  const res = await fetch(`${API_URL}/chat`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    body: JSON.stringify({ userId, schoolId, message, language })
  });
  if (!res.ok) throw new Error('Failed to get AI response');
  return await res.json();
}
