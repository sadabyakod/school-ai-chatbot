import React, { useState, useRef, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useToast } from "../hooks/useToast";
import { submitWrittenAnswers, validateFiles, ApiException } from "../api";

// ==========================================
// TYPES & INTERFACES
// ==========================================

interface WrittenExamUploadProps {
  onSuccess: (submissionId: string, examId: string, studentId: string) => void;
  onBack: () => void;
  toast: ReturnType<typeof useToast>;
}

interface PreviewFile {
  file: File;
  preview: string;
  id: string;
}

// ==========================================
// FILE PREVIEW COMPONENT
// ==========================================

const FilePreview: React.FC<{
  previewFile: PreviewFile;
  onRemove: (id: string) => void;
}> = ({ previewFile, onRemove }) => {
  const isPdf = previewFile.file.name.toLowerCase().endsWith('.pdf');
  
  return (
    <motion.div
      layout
      initial={{ opacity: 0, scale: 0.8 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.8 }}
      className="relative group"
    >
      <div className="w-24 h-24 rounded-xl overflow-hidden border-2 border-gray-200 bg-gray-50 shadow-md group-hover:border-indigo-400 transition-colors">
        {isPdf ? (
          <div className="w-full h-full flex flex-col items-center justify-center bg-gradient-to-br from-red-50 to-orange-50">
            <svg className="w-10 h-10 text-red-500 mb-1" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm2 6a1 1 0 011-1h6a1 1 0 110 2H7a1 1 0 01-1-1zm1 3a1 1 0 100 2h6a1 1 0 100-2H7z" clipRule="evenodd" />
            </svg>
            <span className="text-xs font-medium text-red-600">PDF</span>
          </div>
        ) : (
          <img
            src={previewFile.preview}
            alt="Preview"
            className="w-full h-full object-cover"
          />
        )}
      </div>
      <button
        onClick={() => onRemove(previewFile.id)}
        className="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center shadow-lg opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-600"
      >
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
        </svg>
      </button>
      <div className="absolute bottom-0 left-0 right-0 bg-black/60 text-white text-xs p-1 truncate text-center">
        {previewFile.file.name.slice(0, 10)}...
      </div>
    </motion.div>
  );
};

// ==========================================
// MAIN COMPONENT
// ==========================================

const WrittenExamUpload: React.FC<WrittenExamUploadProps> = ({ onSuccess, onBack, toast }) => {
  const [examId, setExamId] = useState("");
  const [studentId, setStudentId] = useState(() => 
    localStorage.getItem('test-student-id') || 'test-student-001'
  );
  const [files, setFiles] = useState<PreviewFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Handle file selection
  const handleFiles = useCallback((selectedFiles: FileList | File[]) => {
    const newFiles: PreviewFile[] = [];
    const fileArray = Array.from(selectedFiles);
    
    // Validate
    const validation = validateFiles(fileArray);
    if (!validation.valid) {
      toast.showToast(validation.error!, 'error');
      return;
    }

    for (const file of fileArray) {
      const id = `${file.name}-${Date.now()}-${Math.random()}`;
      const preview = file.type.startsWith('image/') 
        ? URL.createObjectURL(file) 
        : '';
      
      newFiles.push({ file, preview, id });
    }

    setFiles(prev => {
      // Check total count
      if (prev.length + newFiles.length > 20) {
        toast.showToast('Maximum 20 files allowed', 'warning');
        return prev;
      }
      return [...prev, ...newFiles];
    });
  }, [toast]);

  // Remove file
  const removeFile = (id: string) => {
    setFiles(prev => {
      const file = prev.find(f => f.id === id);
      if (file?.preview) {
        URL.revokeObjectURL(file.preview);
      }
      return prev.filter(f => f.id !== id);
    });
  };

  // Drag & drop handlers
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    handleFiles(e.dataTransfer.files);
  };

  // Submit handler
  const handleSubmit = async () => {
    if (!examId.trim()) {
      toast.showToast('Please enter an Exam ID', 'warning');
      return;
    }
    if (!studentId.trim()) {
      toast.showToast('Please enter a Student ID', 'warning');
      return;
    }
    if (files.length === 0) {
      toast.showToast('Please select at least one file', 'warning');
      return;
    }

    setUploading(true);
    try {
      const response = await submitWrittenAnswers(
        examId.trim(),
        studentId.trim(),
        files.map(f => f.file)
      );

      toast.showToast('✅ Answer sheets uploaded successfully!', 'success');
      
      // Save student ID for future use
      localStorage.setItem('test-student-id', studentId.trim());
      
      // Navigate to processing screen
      onSuccess(response.writtenSubmissionId, examId.trim(), studentId.trim());
    } catch (err) {
      const error = err as ApiException;
      
      if (error.code === 'DUPLICATE_SUBMISSION') {
        toast.showToast('⚠️ ' + error.message, 'warning');
      } else {
        toast.showToast(error.message || 'Failed to upload', 'error');
      }
    } finally {
      setUploading(false);
    }
  };

  // Format file size
  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const totalSize = files.reduce((acc, f) => acc + f.file.size, 0);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-indigo-50 to-purple-50 py-8 px-4">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-8"
        >
          <button
            onClick={onBack}
            className="flex items-center gap-2 text-gray-600 hover:text-indigo-600 mb-4 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            <span className="font-medium">Back to Exams</span>
          </button>

          <div className="text-center">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-indigo-500 via-purple-600 to-pink-500 rounded-2xl shadow-xl shadow-purple-500/20 mb-4">
              <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 bg-clip-text text-transparent">
              Upload Written Answers
            </h1>
            <p className="text-gray-600 mt-2">
              Upload photos of your answer sheets for AI evaluation
            </p>
          </div>
        </motion.div>

        {/* Form Card */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white rounded-2xl shadow-xl border border-gray-100 p-6"
        >
          {/* Exam ID & Student ID */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Exam ID
              </label>
              <input
                type="text"
                value={examId}
                onChange={(e) => setExamId(e.target.value)}
                placeholder="e.g., EXAM-2024-001"
                className="w-full px-4 py-3 rounded-xl border-2 border-gray-200 focus:border-indigo-500 focus:outline-none transition-colors"
              />
            </div>
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Student ID
              </label>
              <input
                type="text"
                value={studentId}
                onChange={(e) => setStudentId(e.target.value)}
                placeholder="e.g., STU-12345"
                className="w-full px-4 py-3 rounded-xl border-2 border-gray-200 focus:border-indigo-500 focus:outline-none transition-colors"
              />
            </div>
          </div>

          {/* Drag & Drop Zone */}
          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`relative border-2 border-dashed rounded-2xl p-8 text-center cursor-pointer transition-all duration-300 ${
              dragOver
                ? 'border-indigo-500 bg-indigo-50 scale-[1.02]'
                : 'border-gray-300 hover:border-indigo-400 hover:bg-gray-50'
            }`}
          >
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept=".jpg,.jpeg,.png,.webp,.pdf"
              onChange={(e) => e.target.files && handleFiles(e.target.files)}
              className="hidden"
            />
            
            <div className="w-14 h-14 bg-gradient-to-br from-indigo-100 to-purple-100 rounded-2xl flex items-center justify-center mx-auto mb-4">
              <svg className="w-7 h-7 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
            </div>
            
            <p className="text-lg font-semibold text-gray-700 mb-1">
              {dragOver ? 'Drop files here' : 'Drag & drop your answer sheets'}
            </p>
            <p className="text-sm text-gray-500">
              or click to browse • JPG, PNG, WEBP, PDF • Max 10MB each
            </p>
          </div>

          {/* File Previews */}
          <AnimatePresence>
            {files.length > 0 && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="mt-6"
              >
                <div className="flex items-center justify-between mb-3">
                  <span className="text-sm font-semibold text-gray-700">
                    {files.length} file{files.length > 1 ? 's' : ''} selected
                  </span>
                  <span className="text-sm text-gray-500">
                    Total: {formatSize(totalSize)}
                  </span>
                </div>
                
                <div className="flex flex-wrap gap-3">
                  {files.map((f) => (
                    <FilePreview
                      key={f.id}
                      previewFile={f}
                      onRemove={removeFile}
                    />
                  ))}
                </div>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Info Box */}
          <div className="mt-6 p-4 bg-gradient-to-br from-blue-50 to-indigo-50 rounded-xl border border-blue-100">
            <div className="flex items-start gap-3">
              <svg className="w-5 h-5 text-blue-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              <div className="text-sm text-blue-800">
                <p className="font-semibold mb-1">How it works:</p>
                <ul className="space-y-1 text-blue-700">
                  <li>1. Upload photos of your handwritten answers</li>
                  <li>2. Our AI will extract text using OCR</li>
                  <li>3. Get detailed feedback on each answer</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Submit Button */}
          <motion.button
            onClick={handleSubmit}
            disabled={uploading || files.length === 0 || !examId.trim()}
            whileHover={{ scale: uploading ? 1 : 1.02 }}
            whileTap={{ scale: uploading ? 1 : 0.98 }}
            className={`w-full mt-6 py-4 rounded-xl font-bold text-lg flex items-center justify-center gap-3 transition-all duration-300 ${
              uploading || files.length === 0 || !examId.trim()
                ? 'bg-gray-200 text-gray-400 cursor-not-allowed'
                : 'bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white shadow-lg hover:shadow-xl'
            }`}
          >
            {uploading ? (
              <>
                <div className="w-6 h-6 border-3 border-white border-t-transparent rounded-full animate-spin"></div>
                <span>Uploading...</span>
              </>
            ) : (
              <>
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                </svg>
                <span>Submit for Evaluation</span>
              </>
            )}
          </motion.button>
        </motion.div>
      </div>
    </div>
  );
};

export default WrittenExamUpload;
