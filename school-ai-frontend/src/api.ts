// API utility for backend calls
export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';

export async function sendChat({message, token }: {
  message: string;
  language?: string;
  token?: string;
}) {
  const res = await fetch(`${API_URL}/chat`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    body: JSON.stringify({ Question: message })
  });
  if (!res.ok) throw new Error('Failed to get AI response');
  const data = await res.json();
  
  // Backend now returns 'reply' directly for both success and error cases
  return {
    reply: data.reply || "I apologize, but I couldn't generate a response. Please try again.",
    status: data.status
  };
}
