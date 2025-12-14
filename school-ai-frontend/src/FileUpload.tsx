import React, { useState, useMemo, useRef } from "react";
import { uploadFile, uploadQuestionPaper, uploadEvaluationSheet, ApiException } from "./api";
import { useToast } from "./hooks/useToast";
import { motion, AnimatePresence } from "framer-motion";

// Subject options based on class (medium optional)
const getSubjectsForClass = (className: string, _medium?: string): string[] => {
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
    setUploadSuccess(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile?.type === 'application/pdf') {
      setFile(droppedFile);
      setUploadSuccess(false);
    } else {
      toast.warning("Please drop a PDF file only");
    }
  };

  const handleUpload = async () => {
    if (!file || !medium || !className || !subject) {
      toast.warning("Please complete all fields before uploading");
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
      if (uploadType === "model") {
        await uploadQuestionPaper(file, subject, className, medium, state, academicYear, token);
      } else if (uploadType === "evaluation") {
        await uploadEvaluationSheet(file, subject, className, medium, state, academicYear, token);
      } else {
        await uploadFile(file, medium, className, subject, token);
      }
      
      setUploadProgress(100);
      clearInterval(progressInterval);

      const uploadLabel = uploadType === "model" ? "Question paper" : uploadType === "evaluation" ? "Evaluation sheet" : "Syllabus";
      toast.success(`✅ ${uploadLabel} uploaded successfully!`);
      setUploadSuccess(true);
      
      // Reset form after short delay
      setTimeout(() => {
        setFile(null);
        setMedium("");
        setClassName("");
        setSubject("");
        setUploadProgress(0);
        setAcademicYear("");
        setUploadSuccess(false);
        if (fileInputRef.current) fileInputRef.current.value = '';
      }, 2000);
      
    } catch (err) {
      const error = err as ApiException;
      toast.error(error.message || "Upload failed. Please try again.");
      clearInterval(progressInterval);
      setUploadProgress(0);
    } finally {
      setUploading(false);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const isFormComplete = file && medium && className && subject;

  return (
    <div className="max-w-xl mx-auto px-4 py-8">
      <motion.div 
        className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3 }}
      >
        {/* Header */}
        <div className="bg-gradient-to-r from-cyan-500 to-teal-500 px-6 py-5 text-white text-center">
          <h1 className="text-lg font-bold mb-1">
            {uploadType === "model" 
              ? "Upload Question Papers" 
              : uploadType === "evaluation" 
                ? "Upload Answer Keys" 
                : "Upload Syllabus"}
          </h1>
          <p className="text-sm text-white/80">
            {uploadType === "model" 
              ? "For student practice exams" 
              : uploadType === "evaluation"
                ? "For answer evaluation"
                : "To train the AI assistant"}
          </p>
        </div>

        <div className="p-6">
          {/* Upload Type Pills */}
          <div className="flex justify-center gap-2 mb-8">
            {[
              { type: "syllabus", icon: "📚", label: "Syllabus" },
              { type: "model", icon: "📝", label: "Model Papers" },
              { type: "evaluation", icon: "📋", label: "Answer Keys" },
            ].map((item) => (
              <button
                key={item.type}
                onClick={() => setUploadType(item.type)}
                className={`px-4 py-2.5 rounded-full font-medium transition-all text-sm ${
                  uploadType === item.type
                    ? "bg-cyan-500 text-white shadow-md"
                    : "bg-slate-50 text-slate-600 hover:bg-slate-100 border border-slate-200"
                }`}
              >
                <span className="mr-1.5">{item.icon}</span>
                {item.label}
              </button>
            ))}
          </div>

          {/* Academic Details Section */}
          <div className="bg-slate-50 rounded-xl p-5 mb-6">
            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-4">Step 1 · Academic Details</p>
            
            <div className="grid grid-cols-3 gap-4">
              {/* Medium */}
              <div className="form-field">
                <label className="form-label">🌐 Medium</label>
                <select
                  value={medium}
                  onChange={(e) => handleMediumChange(e.target.value)}
                  className="form-select-compact"
                >
                  <option value="">Select</option>
                  <option value="Kannada">Kannada</option>
                  <option value="English">English</option>
                </select>
              </div>

              {/* Class */}
              <div className="form-field">
                <label className="form-label">🎓 Class</label>
                <select
                  value={className}
                  onChange={(e) => handleClassChange(e.target.value)}
                  className="form-select-compact"
                >
                  <option value="">Select</option>
                  {Array.from({ length: 7 }, (_, i) => 6 + i).map((cls) => (
                    <option key={cls} value={String(cls)}>Class {cls}</option>
                  ))}
                </select>
              </div>

              {/* Subject */}
              <div className="form-field">
                <label className="form-label">📖 Subject</label>
                <select
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  disabled={!className}
                  className="form-select-compact"
                >
                  <option value="">{className ? "Select" : "—"}</option>
                  {availableSubjects.map((subj) => (
                    <option key={subj} value={subj}>{subj}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Additional fields for Model Papers / Evaluation Sheets */}
            {(uploadType === "model" || uploadType === "evaluation") && (
              <div className="grid grid-cols-2 gap-4 mt-4 pt-4 border-t border-slate-200">
                <div className="form-field">
                  <label className="form-label">🏛️ Board</label>
                  <select
                    value={state}
                    onChange={(e) => setState(e.target.value)}
                    className="form-select-compact"
                  >
                    <option value="Karnataka">Karnataka</option>
                    <option value="Maharashtra">Maharashtra</option>
                    <option value="TamilNadu">Tamil Nadu</option>
                    <option value="Kerala">Kerala</option>
                    <option value="CBSE">CBSE</option>
                    <option value="ICSE">ICSE</option>
                  </select>
                </div>

                <div className="form-field">
                  <label className="form-label">📅 Year</label>
                  <select
                    value={academicYear}
                    onChange={(e) => setAcademicYear(e.target.value)}
                    className="form-select-compact"
                  >
                    <option value="">Any</option>
                    {Array.from({ length: 10 }, (_, i) => {
                      const year = new Date().getFullYear() - i;
                      return <option key={year} value={`${year}-${(year + 1).toString().slice(-2)}`}>{year}</option>;
                    })}
                  </select>
                </div>
              </div>
            )}
          </div>

          {/* File Upload Section */}
          <div className="mb-6">
            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-4">Step 2 · Upload File</p>
            <motion.div
              className={`relative border-2 border-dashed rounded-xl p-6 text-center transition-all cursor-pointer ${
                isDragOver 
                  ? 'border-cyan-500 bg-cyan-50/50' 
                  : file 
                    ? 'border-emerald-400 bg-emerald-50/50' 
                    : uploadSuccess
                      ? 'border-emerald-500 bg-emerald-50'
                      : 'border-slate-200 bg-white hover:border-cyan-400 hover:bg-cyan-50/20'
              }`}
              onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
              onDragLeave={() => setIsDragOver(false)}
              onDrop={handleDrop}
              onClick={() => !uploadSuccess && fileInputRef.current?.click()}
              whileHover={{ scale: uploadSuccess ? 1 : 1.005 }}
              whileTap={{ scale: uploadSuccess ? 1 : 0.995 }}
            >
              <input 
                ref={fileInputRef}
                type="file" 
                onChange={handleFileChange} 
                className="hidden" 
                accept=".pdf" 
              />
              
              <AnimatePresence mode="wait">
                {uploadSuccess ? (
                  <motion.div
                    key="upload-success"
                    initial={{ opacity: 0, scale: 0.9 }}
                    animate={{ opacity: 1, scale: 1 }}
                    exit={{ opacity: 0, scale: 0.9 }}
                    className="flex flex-col items-center py-2"
                  >
                    <div className="w-12 h-12 bg-emerald-500 rounded-full flex items-center justify-center mb-3">
                      <span className="text-2xl text-white">✓</span>
                    </div>
                    <p className="font-semibold text-emerald-700">Upload Successful!</p>
                    <p className="text-emerald-600 text-sm mt-1">Processing your file...</p>
                  </motion.div>
                ) : file ? (
                  <motion.div
                    key="file-selected"
                    initial={{ opacity: 0, scale: 0.9 }}
                    animate={{ opacity: 1, scale: 1 }}
                    exit={{ opacity: 0, scale: 0.9 }}
                    className="flex flex-col items-center py-2"
                  >
                    <div className="w-12 h-12 bg-emerald-100 rounded-xl flex items-center justify-center mb-3">
                      <span className="text-xl">📄</span>
                    </div>
                    <p className="font-medium text-slate-800">{file.name}</p>
                    <p className="text-slate-500 text-xs mt-1">{formatFileSize(file.size)} • PDF</p>
                    <button
                      onClick={(e) => { e.stopPropagation(); setFile(null); }}
                      className="mt-3 px-3 py-1.5 text-red-500 hover:bg-red-50 rounded-lg text-xs font-medium transition-colors"
                    >
                      Remove
                    </button>
                  </motion.div>
                ) : (
                  <motion.div
                    key="no-file"
                    initial={{ opacity: 0, scale: 0.9 }}
                    animate={{ opacity: 1, scale: 1 }}
                    exit={{ opacity: 0, scale: 0.9 }}
                    className="flex flex-col items-center py-2"
                  >
                    <div className={`w-12 h-12 rounded-xl flex items-center justify-center mb-3 transition-all ${
                      isDragOver ? 'bg-cyan-500 text-white' : 'bg-slate-100'
                    }`}>
                      <span className="text-xl">{isDragOver ? '📥' : '📁'}</span>
                    </div>
                    <p className="font-medium text-slate-700">
                      {isDragOver ? 'Drop here!' : 'Drag & drop PDF'}
                    </p>
                    <p className="text-slate-400 text-xs mt-1">or click to browse</p>
                  </motion.div>
                )}
              </AnimatePresence>
              
              {/* Upload Progress */}
              {uploading && (
                <div className="absolute bottom-0 left-0 right-0 h-1 bg-slate-200 rounded-b-xl overflow-hidden">
                  <motion.div 
                    className="h-full bg-gradient-to-r from-cyan-500 to-teal-500"
                    initial={{ width: 0 }}
                    animate={{ width: `${uploadProgress}%` }}
                    transition={{ duration: 0.3 }}
                  />
                </div>
              )}
            </motion.div>

            {/* Upload Button */}
            <motion.button
              className={`mt-5 w-full py-3 px-5 rounded-xl font-medium transition-all flex items-center justify-center gap-2 ${
                isFormComplete && !uploading && !uploadSuccess
                  ? 'bg-gradient-to-r from-cyan-500 to-teal-600 text-white hover:opacity-90'
                  : 'bg-slate-200 text-slate-400 cursor-not-allowed'
              }`}
              onClick={handleUpload}
              disabled={!isFormComplete || uploading || uploadSuccess}
              whileTap={isFormComplete && !uploading && !uploadSuccess ? { scale: 0.98 } : {}}
            >
              {uploading ? (
                <>
                  <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <span>Uploading {uploadProgress}%</span>
                </>
              ) : uploadSuccess ? (
                <span>✓ Uploaded Successfully</span>
              ) : (
                <span>Upload & Process</span>
              )}
            </motion.button>
          </div>
        </div>
      </motion.div>
    </div>
  );
};

export default FileUpload;
