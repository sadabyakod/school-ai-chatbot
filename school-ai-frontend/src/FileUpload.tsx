import React, { useState, useMemo, useRef } from "react";
import { uploadFile, uploadQuestionPaper, uploadEvaluationSheet, ApiException } from "./api";
import { useToast } from "./hooks/useToast";
import { motion, AnimatePresence } from "framer-motion";

// Subject options based on class (medium optional)
const getSubjectsForClass = (className: string, medium?: string): string[] => {
  // Class 12 has specific subjects
  if (className === "12") {
    return ["Physics", "Chemistry", "Mathematics", "Biology", "Kannada", "English", "Accountancy", "Business Studies", "Economics", "Statistics", "Computer Science", "History", "Political Science"];
  }
  
  // Class 10 has specific subjects
  if (className === "10") {
    return ["English", "Kannada", "Hindi", "Mathematics", "Science", "Social Science", "Sanskrit"];
  }
  
  // Class 11 subjects
  if (className === "11") {
    return ["Physics", "Chemistry", "Mathematics", "Biology", "Kannada", "English", "Accountancy", "Business Studies", "Economics", "Statistics", "Computer Science", "History", "Political Science"];
  }
  
  // Common subjects for other classes (6-9)
  const commonSubjects = ["Mathematics", "Science", "Social Science", "English", "Kannada", "Hindi", "Sanskrit"];
  
  return commonSubjects;
};

const FileUpload: React.FC<{ token?: string; toast: ReturnType<typeof useToast> }> = ({ token, toast }) => {
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [medium, setMedium] = useState<string>("");
  const [className, setClassName] = useState<string>("");
  const [subject, setSubject] = useState<string>("");
  const [isDragOver, setIsDragOver] = useState(false);
  const [uploadSuccess, setUploadSuccess] = useState(false);
  const [uploadType, setUploadType] = useState<string>("syllabus"); // "syllabus", "model", or "evaluation"
  const [state, setState] = useState<string>("Karnataka"); // For model question papers
  const [academicYear, setAcademicYear] = useState<string>(""); // For model question papers
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Get subjects based on selected class (medium is optional)
  const availableSubjects = useMemo(() => {
    if (!className) return [];
    return getSubjectsForClass(className, medium);
  }, [className, medium]);

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

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile?.type === 'application/pdf') {
      setFile(droppedFile);
    } else {
      toast.warning("Please drop a PDF file");
    }
  };

  const handleUpload = async () => {
    if (!file || !medium || !className || !subject) {
      toast.warning("Please fill all fields and select a file.");
      return;
    }
    
    setUploading(true);
    setUploadProgress(0);

    // Simulate progress
    const progressInterval = setInterval(() => {
      setUploadProgress(prev => {
        if (prev >= 90) {
          clearInterval(progressInterval);
          return 90;
        }
        return prev + 10;
      });
    }, 200);
    
    try {
      let data;
      
      if (uploadType === "model") {
        // Upload to Model Question Papers endpoint
        data = await uploadQuestionPaper(file, subject, className, medium, state, academicYear, token);
      } else if (uploadType === "evaluation") {
        // Upload to Evaluation Sheets endpoint
        data = await uploadEvaluationSheet(file, subject, className, medium, state, academicYear, token);
      } else {
        // Upload to Syllabus endpoint
        data = await uploadFile(file, medium, className, subject, token);
      }
      
      setUploadProgress(100);
      clearInterval(progressInterval);

      const uploadLabel = uploadType === "model" ? "Question paper" : uploadType === "evaluation" ? "Evaluation sheet" : "Syllabus";
      toast.success(`${uploadLabel} uploaded successfully: ${data.message || 'Processing started'}`);
      setUploadSuccess(true);
      
      // Reset form
      setFile(null);
      setMedium("");
      setClassName("");
      setSubject("");
      setUploadProgress(0);
      setAcademicYear("");
      
      // Reset file input
      if (fileInputRef.current) fileInputRef.current.value = '';
    } catch (err) {
      const error = err as ApiException;
      toast.error(error.message || "Failed to upload file");
      clearInterval(progressInterval);
      setUploadProgress(0);
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

  const isFormComplete = file && medium && className && subject;

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 py-6 sm:py-8">
      <motion.div 
        className="glass rounded-3xl p-6 sm:p-8 shadow-xl"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <div className="text-center mb-8">
          <motion.div 
            className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-gradient-to-br from-violet-500 via-purple-500 to-fuchsia-500 mb-4 shadow-lg shadow-purple-500/25"
            whileHover={{ scale: 1.05, rotate: 5 }}
          >
            <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
          </motion.div>
          <h2 className="text-2xl sm:text-3xl font-bold text-gradient mb-2">
            {uploadType === "model" 
              ? "Upload Model Question Papers" 
              : uploadType === "evaluation" 
                ? "Upload Evaluation Sheets" 
                : "Upload Syllabus for AI Learning"}
          </h2>
          <p className="text-gray-500">
            {uploadType === "model" 
              ? "Upload model question papers organized by State, Class, and Subject." 
              : uploadType === "evaluation"
                ? "Upload answer evaluation schemes/marking schemes organized by State, Class, and Subject."
                : "Train our AI chatbot by uploading your syllabus PDFs. Follow the steps below to get started."}
          </p>
        </div>

        {/* Upload Type Toggle */}
        <div className="mb-6">
          <div className="flex justify-center gap-2 sm:gap-4 flex-wrap">
            <button
              onClick={() => setUploadType("syllabus")}
              className={`px-4 sm:px-6 py-3 rounded-xl font-semibold transition-all duration-300 text-sm sm:text-base ${
                uploadType === "syllabus"
                  ? "bg-gradient-to-r from-purple-500 to-fuchsia-500 text-white shadow-lg shadow-purple-500/25"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              📚 Syllabus
            </button>
            <button
              onClick={() => setUploadType("model")}
              className={`px-4 sm:px-6 py-3 rounded-xl font-semibold transition-all duration-300 text-sm sm:text-base ${
                uploadType === "model"
                  ? "bg-gradient-to-r from-purple-500 to-fuchsia-500 text-white shadow-lg shadow-purple-500/25"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              📝 Model Papers
            </button>
            <button
              onClick={() => setUploadType("evaluation")}
              className={`px-4 sm:px-6 py-3 rounded-xl font-semibold transition-all duration-300 text-sm sm:text-base ${
                uploadType === "evaluation"
                  ? "bg-gradient-to-r from-purple-500 to-fuchsia-500 text-white shadow-lg shadow-purple-500/25"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              📋 Evaluation Sheet
            </button>
          </div>
        </div>

        {/* Step-by-Step Guidance */}
        <div className="mb-8">
          <div className="flex items-center justify-center gap-4 text-sm text-gray-600">
            <div className={`flex items-center gap-2 ${medium ? 'text-blue-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${medium ? 'bg-blue-100' : 'bg-gray-100'}`}>1</span>
              <span>Select Medium</span>
            </div>
            <div className="w-8 h-0.5 bg-gray-200 rounded"></div>
            <div className={`flex items-center gap-2 ${className ? 'text-blue-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${className ? 'bg-blue-100' : 'bg-gray-100'}`}>2</span>
              <span>Choose Class</span>
            </div>
            <div className="w-8 h-0.5 bg-gray-200 rounded"></div>
            <div className={`flex items-center gap-2 ${subject ? 'text-blue-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${subject ? 'bg-blue-100' : 'bg-gray-100'}`}>3</span>
              <span>Pick Subject</span>
            </div>
            <div className="w-8 h-0.5 bg-gray-200 rounded"></div>
            <div className={`flex items-center gap-2 ${file ? 'text-blue-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${file ? 'bg-blue-100' : 'bg-gray-100'}`}>4</span>
              <span>Upload PDF</span>
            </div>
          </div>
        </div>

        {/* Selection Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
          {/* Medium Dropdown */}
          <div className="relative">
            <label className="block text-sm font-semibold text-gray-700 mb-2">
              <span className="flex items-center gap-2">
                🌐 Medium
              </span>
            </label>
            <select
              value={medium}
              onChange={(e) => handleMediumChange(e.target.value)}
              className="w-full px-4 py-3 bg-white/80 border-2 border-gray-200 rounded-xl text-gray-800 font-medium focus:outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 transition-all duration-300 appearance-none cursor-pointer hover:border-purple-300"
            >
              <option value="">Select Medium</option>
              <option value="Kannada">🇮🇳 Kannada</option>
              <option value="English">🇬🇧 English</option>
            </select>
            <div className="absolute right-4 top-11 pointer-events-none text-gray-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>

          {/* Class Dropdown */}
          <div className="relative">
            <label className="block text-sm font-semibold text-gray-700 mb-2">
              <span className="flex items-center gap-2">
                🎓 Class
              </span>
            </label>
            <select
              value={className}
              onChange={(e) => handleClassChange(e.target.value)}
              className="w-full px-4 py-3 bg-white/80 border-2 border-gray-200 rounded-xl text-gray-800 font-medium focus:outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 transition-all duration-300 appearance-none cursor-pointer hover:border-purple-300"
            >
              <option value="">Select Class</option>
              {Array.from({ length: 7 }, (_, i) => 6 + i).map((cls) => (
                <option key={cls} value={String(cls)}>📚 Class {cls}</option>
              ))}
            </select>
            <div className="absolute right-4 top-11 pointer-events-none text-gray-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>

          {/* Subject Dropdown */}
          <div className="relative">
            <label className="block text-sm font-semibold text-gray-700 mb-2">
              <span className="flex items-center gap-2">
                📖 Subject
              </span>
            </label>
            <select
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className={`w-full px-4 py-3 bg-white/80 border-2 rounded-xl text-gray-800 font-medium focus:outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 transition-all duration-300 appearance-none cursor-pointer ${
                !className 
                  ? 'border-gray-100 bg-gray-50 text-gray-400 cursor-not-allowed' 
                  : 'border-gray-200 hover:border-purple-300'
              }`}
              disabled={!className}
            >
              <option value="">Select Subject</option>
              {availableSubjects.map((subj) => (
                <option key={subj} value={subj}>✨ {subj}</option>
              ))}
            </select>
            <div className="absolute right-4 top-11 pointer-events-none text-gray-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>
        </div>

        {/* Additional fields for Model Question Papers and Evaluation Sheets */}
        {(uploadType === "model" || uploadType === "evaluation") && (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
            {/* State Dropdown */}
            <div className="relative">
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                <span className="flex items-center gap-2">
                  🏛️ State/Board
                </span>
              </label>
              <select
                value={state}
                onChange={(e) => setState(e.target.value)}
                className="w-full px-4 py-3 bg-white/80 border-2 border-gray-200 rounded-xl text-gray-800 font-medium focus:outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 transition-all duration-300 appearance-none cursor-pointer hover:border-purple-300"
              >
                <option value="Karnataka">🇮🇳 Karnataka</option>
                <option value="Maharashtra">🇮🇳 Maharashtra</option>
                <option value="TamilNadu">🇮🇳 Tamil Nadu</option>
                <option value="Kerala">🇮🇳 Kerala</option>
                <option value="AndhraPradesh">🇮🇳 Andhra Pradesh</option>
                <option value="Telangana">🇮🇳 Telangana</option>
                <option value="CBSE">📘 CBSE</option>
                <option value="ICSE">📗 ICSE</option>
              </select>
              <div className="absolute right-4 top-11 pointer-events-none text-gray-400">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>

            {/* Academic Year Dropdown */}
            <div className="relative">
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                <span className="flex items-center gap-2">
                  📅 Academic Year (Optional)
                </span>
              </label>
              <select
                value={academicYear}
                onChange={(e) => setAcademicYear(e.target.value)}
                className="w-full px-4 py-3 bg-white/80 border-2 border-gray-200 rounded-xl text-gray-800 font-medium focus:outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 transition-all duration-300 appearance-none cursor-pointer hover:border-purple-300"
              >
                <option value="">Select Year</option>
                {Array.from({ length: 10 }, (_, i) => {
                  const year = new Date().getFullYear() - i;
                  return (
                    <option key={year} value={`${year}-${(year + 1).toString().slice(-2)}`}>
                      📆 {year}-{(year + 1).toString().slice(-2)}
                    </option>
                  );
                })}
              </select>
              <div className="absolute right-4 top-11 pointer-events-none text-gray-400">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>
          </div>
        )}

        {/* File Drop Zone */}
        <motion.div
          className={`relative border-2 border-dashed rounded-2xl p-8 sm:p-12 text-center transition-all duration-300 cursor-pointer ${
            isDragOver 
              ? 'border-purple-500 bg-purple-50/50' 
              : file 
                ? 'border-green-400 bg-green-50/50' 
                : 'border-gray-300 bg-gray-50/50 hover:border-purple-400 hover:bg-purple-50/30'
          }`}
          onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
          onDragLeave={() => setIsDragOver(false)}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          whileHover={{ scale: 1.01 }}
          whileTap={{ scale: 0.99 }}
        >
          <input 
            ref={fileInputRef}
            type="file" 
            onChange={handleFileChange} 
            className="hidden" 
            accept=".pdf" 
          />
          
          <AnimatePresence mode="wait">
            {file ? (
              <motion.div
                key="file-selected"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.9 }}
                className="flex flex-col items-center"
              >
                <div className="w-12 h-12 bg-gradient-to-br from-green-400 to-emerald-500 rounded-2xl flex items-center justify-center mb-4 shadow-lg shadow-green-500/25">
                  <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <p className="font-semibold text-gray-800 text-lg">{file.name}</p>
                <p className="text-gray-500 text-sm mt-1">{formatFileSize(file.size)}</p>
                <button
                  onClick={(e) => { e.stopPropagation(); setFile(null); }}
                  className="mt-3 text-red-500 hover:text-red-600 text-sm font-medium flex items-center gap-1"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                  Remove file
                </button>
              </motion.div>
            ) : (
              <motion.div
                key="no-file"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.9 }}
                className="flex flex-col items-center"
              >
                <div className={`w-12 h-12 rounded-2xl flex items-center justify-center mb-4 transition-all duration-300 ${
                  isDragOver 
                    ? 'bg-gradient-to-br from-purple-500 to-fuchsia-500 shadow-lg shadow-purple-500/25' 
                    : 'bg-gray-200'
                }`}>
                  <svg className={`w-8 h-8 ${isDragOver ? 'text-white' : 'text-gray-400'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                  </svg>
                </div>
                <p className="font-semibold text-gray-700 text-lg">
                  {isDragOver ? 'Drop your PDF here!' : 'Drag & drop your syllabus PDF here'}
                </p>
                <p className="text-gray-500 text-sm mt-1">or click to browse</p>
                <p className="text-gray-400 text-xs mt-3 flex items-center gap-1">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Supports PDF files only
                </p>
              </motion.div>
            )}
          </AnimatePresence>
        </motion.div>

        {/* Upload Button */}
        <motion.button
          className={`mt-6 w-full py-4 px-6 rounded-xl font-semibold text-lg transition-all duration-300 flex items-center justify-center gap-3 ${
            isFormComplete && !uploading
              ? 'bg-gradient-to-r from-violet-500 via-purple-500 to-fuchsia-500 text-white shadow-lg shadow-purple-500/25 hover:shadow-xl hover:shadow-purple-500/30 hover:scale-[1.02]'
              : 'bg-gray-200 text-gray-400 cursor-not-allowed'
          }`}
          onClick={handleUpload}
          disabled={!isFormComplete || uploading}
          whileHover={isFormComplete && !uploading ? { scale: 1.02 } : {}}
          whileTap={isFormComplete && !uploading ? { scale: 0.98 } : {}}
        >
          {uploading ? (
            <>
              <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              <span>Uploading... {uploadProgress}%</span>
            </>
          ) : (
            <>
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
              <span>Upload & Process</span>
            </>
          )}
        </motion.button>

        {/* Progress Steps */}
        {/* What happens next? */}
        <div className="mt-8 p-4 bg-blue-50 rounded-xl border border-blue-200">
          <h3 className="text-sm font-semibold text-blue-900 mb-2 flex items-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            What happens next?
          </h3>
          <ul className="text-xs text-blue-800 space-y-1">
            <li>• Your syllabus will be processed and analyzed by our AI</li>
            <li>• The content will be used to train the chatbot for better responses</li>
            <li>• You'll receive a confirmation once processing is complete</li>
            <li>• Your data is securely stored and used only for educational purposes</li>
          </ul>
        </div>

        <div className="mt-8 pt-6 border-t border-gray-200/50">
          <div className="flex items-center justify-between text-sm">
            <div className={`flex items-center gap-2 ${medium && className && subject ? 'text-green-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${medium && className && subject ? 'bg-green-100' : 'bg-gray-100'}`}>
                {medium && className && subject ? '✓' : '1'}
              </span>
              <span className="hidden sm:inline">Configure</span>
            </div>
            <div className="flex-1 h-0.5 mx-2 bg-gray-200 rounded">
              <div className={`h-full rounded transition-all duration-500 ${medium && className && subject ? 'w-full bg-green-400' : 'w-0'}`} />
            </div>
            <div className={`flex items-center gap-2 ${file ? 'text-green-600' : 'text-gray-400'}`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${file ? 'bg-green-100' : 'bg-gray-100'}`}>
                {file ? '✓' : '2'}
              </span>
              <span className="hidden sm:inline">Select File</span>
            </div>
            <div className="flex-1 h-0.5 mx-2 bg-gray-200 rounded">
              <div className={`h-full rounded transition-all duration-500 ${file ? 'w-full bg-green-400' : 'w-0'}`} />
            </div>
            <div className={`flex items-center gap-2 text-gray-400`}>
              <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold bg-gray-100`}>
                3
              </span>
              <span className="hidden sm:inline">Upload</span>
            </div>
          </div>
        </div>
      </motion.div>
    </div>
  );
};

export default FileUpload;
