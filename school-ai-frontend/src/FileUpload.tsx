import React, { useState } from "react";
import { buildApiUrl } from "./api";

const FileUpload: React.FC<{ token?: string }> = ({ token }) => {
  const [file, setFile] = useState<File | null>(null);
  const [status, setStatus] = useState<string>("");
  const [Class, setClass] = useState<string>("");
  const [subject, setSubject] = useState<string>("");
  const [chapter, setChapter] = useState<string>("");

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFile(e.target.files?.[0] || null);
  };

  const handleUpload = async () => {
  if (!file || !Class || !subject || !chapter) {
      setStatus("Please fill all fields and select a file.");
      return;
    }
    setStatus("Uploading...");
    const formData = new FormData();
    formData.append("file", file);
    formData.append("className", Class);
    formData.append("subject", subject);
    formData.append("chapter", chapter);
    try {
      const res = await fetch(buildApiUrl('/upload/textbook'), {
        method: "POST",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      });
      if (!res.ok) throw new Error("Upload failed");
      const data = await res.json();
      setStatus(`Success: ${data.message}`);
    } catch (err) {
      setStatus("Error uploading file");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-8 bg-white p-6 rounded shadow">
      <h2 className="text-lg font-bold mb-4">Upload Syllabus/FAQ PDF</h2>
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Class</label>
        <select
          value={Class}
            onChange={(e) => setClass(e.target.value)}
          className="w-full border rounded px-3 py-2"
        >
          <option value="">Select Class</option>
          {Array.from({ length: 7 }, (_, i) => 6 + i).map((cls) => (
            <option key={cls} value={cls}>{`Class ${cls}`}</option>
          ))}
        </select>
      </div>
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Subject</label>
        <input
          type="text"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          className="w-full border rounded px-3 py-2"
          placeholder="Enter Subject"
        />
      </div>
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Chapter</label>
        <input
          type="text"
          value={chapter}
          onChange={(e) => setChapter(e.target.value)}
          className="w-full border rounded px-3 py-2"
          placeholder="Enter Chapter"
        />
      </div>
      <input type="file" onChange={handleFileChange} className="mb-2" />
      <button
        className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:opacity-50"
        onClick={handleUpload}
  disabled={!file || !Class || !subject || !chapter}
      >
        Upload
      </button>
      {status && <div className="mt-2 text-sm">{status}</div>}
    </div>
  );
};

export default FileUpload;
