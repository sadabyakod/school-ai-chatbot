import React, { useState, useMemo } from "react";
import { uploadFile, ApiException } from "./api";
import { useToast } from "./hooks/useToast";

// Subject options based on medium and class
const getSubjectsForClass = (medium: string, className: string): string[] => {
  // Class 12 has specific subjects
  if (className === "12") {
    return ["Physics", "Chemistry", "Mathematics", "Biology", "Kannada", "English"];
  }
  
  // Class 10 has specific subjects
  if (className === "10") {
    return ["English", "Kannada", "Hindi", "Mathematics", "Science", "Social Science"];
  }
  
  // Common subjects for other classes
  const commonSubjects = ["Mathematics", "Science", "Social Science", "English"];
  
  // Add Kannada for Kannada medium, or Hindi for English medium
  if (medium === "Kannada") {
    return ["Kannada", ...commonSubjects];
  } else {
    return ["Hindi", ...commonSubjects];
  }
};

const FileUpload: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [medium, setMedium] = useState<string>("");
  const [className, setClassName] = useState<string>("");
  const [subject, setSubject] = useState<string>("");

  // Get subjects based on selected medium and class
  const availableSubjects = useMemo(() => {
    if (!medium || !className) return [];
    return getSubjectsForClass(medium, className);
  }, [medium, className]);

  // Reset subject when medium or class changes
  const handleMediumChange = (value: string) => {
    setMedium(value);
    setSubject("");
  };

  const handleClassChange = (value: string) => {
    setClassName(value);
    setSubject("");
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFile(e.target.files?.[0] || null);
  };

  const handleUpload = async () => {
    if (!file || !medium || !className || !subject) {
      toast.warning("Please fill all fields and select a file.");
      return;
    }
    
    setUploading(true);
    
    try {
      const data = await uploadFile(file, medium, className, subject, token);
      
      toast.success(`File uploaded successfully: ${data.message || 'Processing started'}`);
      
      // Reset form
      setFile(null);
      setMedium("");
      setClassName("");
      setSubject("");
      
      // Reset file input
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      if (fileInput) fileInput.value = '';
    } catch (err) {
      const error = err as ApiException;
      toast.error(error.message || "Failed to upload file");
    } finally {
      setUploading(false);
    }
  };

  // Format file size for display
  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div className="max-w-md mx-auto mt-8 bg-white p-6 rounded shadow">
      <h2 className="text-lg font-bold mb-4">Upload Syllabus/FAQ PDF</h2>
      
      {/* Medium Dropdown */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Medium</label>
        <select
          value={medium}
          onChange={(e) => handleMediumChange(e.target.value)}
          className="w-full border rounded px-3 py-2"
        >
          <option value="">Select Medium</option>
          <option value="Kannada">Kannada</option>
          <option value="English">English</option>
        </select>
      </div>

      {/* Class Dropdown */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Class</label>
        <select
          value={className}
          onChange={(e) => handleClassChange(e.target.value)}
          className="w-full border rounded px-3 py-2"
        >
          <option value="">Select Class</option>
          {Array.from({ length: 7 }, (_, i) => 6 + i).map((cls) => (
            <option key={cls} value={String(cls)}>{`Class ${cls}`}</option>
          ))}
        </select>
      </div>

      {/* Subject Dropdown */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">Subject</label>
        <select
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          className="w-full border rounded px-3 py-2"
          disabled={!medium || !className}
        >
          <option value="">Select Subject</option>
          {availableSubjects.map((subj) => (
            <option key={subj} value={subj}>{subj}</option>
          ))}
        </select>
      </div>

      {/* File Input */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">File</label>
        <input type="file" onChange={handleFileChange} className="w-full" accept=".pdf" />
        {file && (
          <p className="text-sm text-gray-500 mt-1">
            Selected: {file.name} ({formatFileSize(file.size)})
          </p>
        )}
      </div>

      {/* Upload Button */}
      <button
        className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:opacity-50 w-full"
        onClick={handleUpload}
        disabled={!file || !medium || !className || !subject || uploading}
      >
        {uploading ? 'Uploading...' : 'Upload'}
      </button>
    </div>
  );
};

export default FileUpload;
