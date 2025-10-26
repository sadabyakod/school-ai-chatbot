import React from "react";

const Auth: React.FC<{ onAuth: (token: string) => void }> = () => {
  // Authentication temporarily disabled
  return (
    <div className="max-w-md mx-auto mt-8 bg-white p-6 rounded shadow">
      <h2 className="text-lg font-bold mb-4">Authentication Disabled</h2>
      <div className="text-gray-600">Login and registration are temporarily unavailable.</div>
    </div>
  );
};

export default Auth;
