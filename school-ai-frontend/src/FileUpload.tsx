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
  const completedSteps = [medium, className, subject, file].filter(Boolean).length;

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 py-6">
      <motion.div 
        className="card p-6 sm:p-8"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
      >
        {/* Header */}
        <div className="text-center mb-6">
          <h2 className="text-xl font-bold text-slate-800 mb-1">
            {uploadType === "model" 
              ? "📝 Upload Question Papers" 
              : uploadType === "evaluation" 
                ? "📋 Upload Evaluation Sheets" 
                : "📤 Upload Syllabus Materials"}
          </h2>
          <p className="text-sm text-slate-500">
            {uploadType === "model" 
              ? "Add model question papers for practice" 
              : uploadType === "evaluation"
                ? "Add marking schemes for evaluation"
                : "Train the AI with your syllabus content"}
          </p>
        </div>

        {/* Upload Type Toggle */}
        <div className="mb-6 pb-6 border-b border-slate-100">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide text-center mb-3">Upload Type</p>
          <div className="flex justify-center gap-2 flex-wrap">
            {[
              { type: "syllabus", icon: "📚", label: "Syllabus" },
              { type: "model", icon: "📝", label: "Model Papers" },
              { type: "evaluation", icon: "📋", label: "Answer Keys" },
            ].map((item) => (
              <button
                key={item.type}
                onClick={() => setUploadType(item.type)}
                className={`px-4 py-2 rounded-lg font-medium transition-all text-sm ${
                  uploadType === item.type
                    ? "bg-cyan-500 text-white shadow-md"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                <span className="mr-1">{item.icon}</span>
                {item.label}
              </button>
            ))}
          </div>
        </div>

        {/* Selection Section */}
        <div className="mb-6 pb-6 border-b border-slate-100">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide text-center mb-4">Select Details</p>
          
          {/* Main Selection Row */}
          <div className="flex flex-wrap gap-3 justify-center mb-4">
            {/* Medium Dropdown */}
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1 text-center">Medium</label>
              <select
                value={medium}
                onChange={(e) => handleMediumChange(e.target.value)}
                className="form-select"
              >
                <option value="">Select</option>
                <option value="Kannada">Kannada</option>
                <option value="English">English</option>
              </select>
            </div>

            {/* Class Dropdown */}
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1 text-center">Class</label>
              <select
                value={className}
                onChange={(e) => handleClassChange(e.target.value)}
                className="form-select"
              >
                <option value="">Select</option>
                {Array.from({ length: 7 }, (_, i) => 6 + i).map((cls) => (
                  <option key={cls} value={String(cls)}>Class {cls}</option>
                ))}
              </select>
            </div>

            {/* Subject Dropdown */}
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1 text-center">Subject</label>
              <select
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                disabled={!className}
                className="form-select"
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
            <div className="flex flex-wrap gap-3 justify-center pt-3 border-t border-slate-100">
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1 text-center">State/Board</label>
                <select
                  value={state}
                  onChange={(e) => setState(e.target.value)}
                  className="form-select"
                >
                  <option value="Karnataka">Karnataka</option>
                  <option value="Maharashtra">Maharashtra</option>
                  <option value="TamilNadu">Tamil Nadu</option>
                  <option value="Kerala">Kerala</option>
                  <option value="CBSE">CBSE</option>
                  <option value="ICSE">ICSE</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1 text-center">Year</label>
                <select
                  value={academicYear}
                  onChange={(e) => setAcademicYear(e.target.value)}
                  className="form-select"
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

        {/* File Drop Zone */}
        <motion.div
          className={`relative border-2 border-dashed rounded-2xl p-8 sm:p-10 text-center transition-all cursor-pointer ${
            isDragOver 
              ? 'border-cyan-500 bg-cyan-50/50' 
              : file 
                ? 'border-emerald-400 bg-emerald-50/50' 
                : uploadSuccess
                  ? 'border-emerald-500 bg-emerald-50'
                  : 'border-slate-300 bg-slate-50/50 hover:border-cyan-400 hover:bg-cyan-50/30'
          }`}
          onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
          onDragLeave={() => setIsDragOver(false)}
          onDrop={handleDrop}
          onClick={() => !uploadSuccess && fileInputRef.current?.click()}
          whileHover={{ scale: uploadSuccess ? 1 : 1.01 }}
          whileTap={{ scale: uploadSuccess ? 1 : 0.99 }}
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
                className="flex flex-col items-center py-4"
              >
                <motion.div 
                  className="w-16 h-16 bg-gradient-to-br from-emerald-400 to-green-500 rounded-full flex items-center justify-center mb-4 shadow-lg shadow-green-500/25"
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ type: "spring", bounce: 0.5 }}
                >
                  <span className="text-3xl">✅</span>
                </motion.div>
                <p className="font-bold text-emerald-700 text-xl">Upload Successful!</p>
                <p className="text-emerald-600 text-sm mt-1">Your file is now being processed</p>
              </motion.div>
            ) : file ? (
              <motion.div
                key="file-selected"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.9 }}
                className="flex flex-col items-center"
              >
                <div className="w-14 h-14 bg-gradient-to-br from-emerald-400 to-green-500 rounded-2xl flex items-center justify-center mb-4 shadow-lg shadow-green-500/25">
                  <span className="text-2xl">📄</span>
                </div>
                <p className="font-semibold text-slate-800 text-lg">{file.name}</p>
                <p className="text-slate-500 text-sm mt-1">{formatFileSize(file.size)} • PDF Document</p>
                <button
                  onClick={(e) => { e.stopPropagation(); setFile(null); }}
                  className="mt-4 px-4 py-2 text-red-500 hover:bg-red-50 rounded-lg text-sm font-medium flex items-center gap-1.5 transition-colors"
                >
                  <span>🗑️</span>
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
                <div className={`w-14 h-14 rounded-2xl flex items-center justify-center mb-4 transition-all ${
                  isDragOver 
                    ? 'bg-gradient-to-br from-cyan-500 to-teal-500 shadow-lg shadow-cyan-500/25' 
                    : 'bg-slate-200'
                }`}>
                  <span className="text-3xl">{isDragOver ? '📥' : '📁'}</span>
                </div>
                <p className="font-semibold text-slate-700 text-lg">
                  {isDragOver ? 'Drop your PDF here!' : 'Drag & drop your PDF here'}
                </p>
                <p className="text-slate-500 text-sm mt-1">or click anywhere to browse</p>
                <div className="mt-4 inline-flex items-center gap-2 px-3 py-1.5 bg-slate-100 rounded-full text-xs text-slate-500">
                  <span>📄</span>
                  PDF files only • Max 50MB
                </div>
              </motion.div>
            )}
          </AnimatePresence>
          
          {/* Upload Progress */}
          {uploading && (
            <div className="absolute bottom-0 left-0 right-0 h-1 bg-slate-200 rounded-b-2xl overflow-hidden">
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
          className={`mt-6 w-full py-4 px-6 rounded-xl font-semibold text-lg transition-all flex items-center justify-center gap-3 ${
            isFormComplete && !uploading && !uploadSuccess
              ? 'bg-gradient-to-r from-cyan-500 to-teal-600 text-white shadow-lg shadow-cyan-500/25 hover:shadow-xl hover:shadow-cyan-500/30 hover:scale-[1.02]'
              : 'bg-slate-200 text-slate-400 cursor-not-allowed'
          }`}
          onClick={handleUpload}
          disabled={!isFormComplete || uploading || uploadSuccess}
          whileHover={isFormComplete && !uploading && !uploadSuccess ? { scale: 1.02 } : {}}
          whileTap={isFormComplete && !uploading && !uploadSuccess ? { scale: 0.98 } : {}}
        >
          {uploading ? (
            <>
              <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              <span>Uploading... {uploadProgress}%</span>
            </>
          ) : uploadSuccess ? (
            <>
              <span>✅</span>
              <span>Uploaded Successfully!</span>
            </>
          ) : (
            <>
              <span>📤</span>
              <span>Upload & Process</span>
            </>
          )}
        </motion.button>

        {/* What happens next? */}
        <div className="mt-8 info-box">
          <h3 className="text-sm font-semibold text-cyan-800 mb-3 flex items-center gap-2">
            <span>💡</span>
            What happens after upload?
          </h3>
          <ul className="text-sm text-cyan-700 space-y-2">
            <li className="flex items-start gap-2">
              <span className="text-emerald-500 mt-0.5">✓</span>
              <span>Your PDF is securely processed and analyzed</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-500 mt-0.5">✓</span>
              <span>Content is added to the AI's knowledge base</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-500 mt-0.5">✓</span>
              <span>Students can ask questions about this material</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-500 mt-0.5">✓</span>
              <span>Answers stay 100% within syllabus boundaries</span>
            </li>
          </ul>
        </div>

        {/* Bottom Progress Summary */}
        <div className="mt-6 pt-6 border-t border-slate-200/50">
          <div className="flex items-center justify-center gap-3 text-sm">
            <span className={`font-medium ${completedSteps === 4 ? 'text-emerald-600' : 'text-slate-500'}`}>
              {completedSteps}/4 steps completed
            </span>
            <div className="w-24 h-2 bg-slate-200 rounded-full overflow-hidden">
              <div 
                className="h-full bg-gradient-to-r from-cyan-500 to-emerald-500 rounded-full transition-all duration-500"
                style={{ width: `${(completedSteps / 4) * 100}%` }}
              />
            </div>
          </div>
        </div>
      </motion.div>
    </div>
  );
};

export default FileUpload;
