// API utility for backend calls
// ASP.NET Core backend - Production: Azure App Service
export const API_URL = import.meta.env.VITE_API_URL || 'https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net';

// Helper to build API URLs
export const buildApiUrl = (endpoint: string): string => {
  return `${API_URL}${endpoint}`;
};

// ==========================================
// RETRY & ERROR HANDLING UTILITIES
// ==========================================

export interface ApiError {
  message: string;
  status?: number;
  code?: string;
  details?: any;
}

export class ApiException extends Error {
  status?: number;
  code?: string;
  details?: any;

  constructor(error: ApiError) {
    super(error.message);
    this.name = 'ApiException';
    this.status = error.status;
    this.code = error.code;
    this.details = error.details;
  }
}

interface RetryOptions {
  maxRetries?: number;
  retryDelay?: number;
  retryOn?: number[]; // HTTP status codes to retry on
  exponentialBackoff?: boolean;
}

const DEFAULT_RETRY_OPTIONS: RetryOptions = {
  maxRetries: 5,
  retryDelay: 2000,
  retryOn: [408, 429, 500, 502, 503, 504],
  exponentialBackoff: true,
};

/**
 * Delays execution for specified milliseconds
 */
const delay = (ms: number): Promise<void> => 
  new Promise(resolve => setTimeout(resolve, ms));

/**
 * Retry wrapper with exponential backoff
 */
async function fetchWithRetry(
  url: string,
  options: RequestInit = {},
  retryOptions: RetryOptions = {}
): Promise<Response> {
  const opts = { ...DEFAULT_RETRY_OPTIONS, ...retryOptions };
  let lastError: Error | null = null;

  for (let attempt = 0; attempt <= (opts.maxRetries || 0); attempt++) {
    try {
      const response = await fetch(url, options);

      // If response is OK or error is not retryable, return immediately
      if (response.ok || !opts.retryOn?.includes(response.status)) {
        return response;
      }

      // If this is the last attempt, return the response (will be handled as error)
      if (attempt === opts.maxRetries) {
        return response;
      }

      // Calculate delay with exponential backoff
      const delayMs = opts.exponentialBackoff
        ? (opts.retryDelay || 1000) * Math.pow(2, attempt)
        : (opts.retryDelay || 1000);

      console.warn(
        `Attempt ${attempt + 1}/${(opts.maxRetries || 0) + 1} failed with status ${response.status}. Retrying in ${delayMs}ms...`
      );

      await delay(delayMs);
    } catch (error) {
      lastError = error as Error;

      // If this is the last attempt, throw the error
      if (attempt === opts.maxRetries) {
        throw new ApiException({
          message: `Network request failed after ${attempt + 1} attempts: ${lastError.message}`,
          code: 'NETWORK_ERROR',
          details: lastError,
        });
      }

      // Calculate delay
      const delayMs = opts.exponentialBackoff
        ? (opts.retryDelay || 1000) * Math.pow(2, attempt)
        : (opts.retryDelay || 1000);

      console.warn(
        `Network error on attempt ${attempt + 1}/${(opts.maxRetries || 0) + 1}: ${lastError.message}. Retrying in ${delayMs}ms...`
      );

      await delay(delayMs);
    }
  }

  throw new ApiException({
    message: lastError?.message || 'Unknown error occurred',
    code: 'MAX_RETRIES_EXCEEDED',
  });
}

/**
 * Parse error response from API
 */
async function parseErrorResponse(response: Response): Promise<ApiError> {
  let errorMessage = `Request failed with status ${response.status}`;
  let errorDetails: any = null;

  try {
    const contentType = response.headers.get('content-type');
    if (contentType?.includes('application/json')) {
      const errorData = await response.json();
      errorMessage = errorData.message || errorData.error || errorData.title || errorMessage;
      errorDetails = errorData;
    } else {
      const text = await response.text();
      if (text) errorMessage = text;
    }
  } catch (e) {
    // Failed to parse error response, use default message
  }

  return {
    message: errorMessage,
    status: response.status,
    code: `HTTP_${response.status}`,
    details: errorDetails,
  };
}

// ==========================================
// API METHODS
// ==========================================

export async function sendChat({message, token }: {
  message: string;
  language?: string;
  token?: string;
}) {
  const response = await fetchWithRetry(
    buildApiUrl('/api/chat'),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ Question: message })
    },
    { maxRetries: 2, retryDelay: 1000 } // Custom retry for chat
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  const data = await response.json();
  
  return {
    reply: data.reply || "I apologize, but I couldn't generate a response. Please try again.",
    status: data.status,
    sessionId: data.sessionId,
  };
}

// ==========================================
// CHUNKED FILE UPLOAD FOR LARGE FILES
// ==========================================

const CHUNK_SIZE = 5 * 1024 * 1024; // 5MB chunks
const MAX_SIMPLE_UPLOAD_SIZE = 10 * 1024 * 1024; // 10MB - use simple upload for smaller files

export interface UploadProgress {
  loaded: number;
  total: number;
  percentage: number;
  status: 'preparing' | 'uploading' | 'processing' | 'completed' | 'error';
  message: string;
}

export type ProgressCallback = (progress: UploadProgress) => void;

/**
 * Generate a unique upload ID for tracking chunked uploads
 */
function generateUploadId(): string {
  return `upload_${Date.now()}_${Math.random().toString(36).substring(2, 15)}`;
}

/**
 * Upload a single chunk to the server
 */
async function uploadChunk(
  chunk: Blob,
  uploadId: string,
  chunkIndex: number,
  totalChunks: number,
  fileName: string,
  medium: string,
  className: string,
  subject: string,
  token?: string
): Promise<Response> {
  const formData = new FormData();
  formData.append('chunk', chunk);
  formData.append('uploadId', uploadId);
  formData.append('chunkIndex', chunkIndex.toString());
  formData.append('totalChunks', totalChunks.toString());
  formData.append('fileName', fileName);
  formData.append('medium', medium);
  formData.append('className', className);
  formData.append('subject', subject);

  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(buildApiUrl('/api/file/upload-chunk'), {
    method: 'POST',
    headers: headers,
    body: formData
  });

  return response;
}

/**
 * Finalize chunked upload - tells server to assemble chunks
 */
async function finalizeChunkedUpload(
  uploadId: string,
  fileName: string,
  totalChunks: number,
  fileSize: number,
  medium: string,
  className: string,
  subject: string,
  token?: string
): Promise<Response> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json'
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(buildApiUrl('/api/file/finalize-upload'), {
    method: 'POST',
    headers: headers,
    body: JSON.stringify({
      uploadId,
      fileName,
      totalChunks,
      fileSize,
      medium,
      className,
      subject
    })
  });

  return response;
}

/**
 * Upload file with chunking support for large files
 * Uses simple upload for files < 10MB, chunked for larger files
 */
export async function uploadFileWithProgress(
  file: File,
  medium: string,
  className: string,
  subject: string,
  onProgress?: ProgressCallback,
  token?: string
): Promise<any> {
  const updateProgress = (progress: Partial<UploadProgress>) => {
    if (onProgress) {
      onProgress({
        loaded: 0,
        total: file.size,
        percentage: 0,
        status: 'preparing',
        message: 'Preparing upload...',
        ...progress
      });
    }
  };

  // For smaller files, use simple upload
  if (file.size <= MAX_SIMPLE_UPLOAD_SIZE) {

    updateProgress({ status: 'uploading', message: 'Uploading file...', percentage: 0 });
    
    const result = await uploadFile(file, medium, className, subject, token);
    
    updateProgress({ 
      status: 'completed', 
      message: 'Upload complete!', 
      percentage: 100,
      loaded: file.size 
    });
    
    return result;
  }

  // For large files, use chunked upload
  const uploadId = generateUploadId();
  const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
  
  updateProgress({ 
    status: 'preparing', 
    message: `Preparing to upload ${totalChunks} chunks...`,
    percentage: 0
  });

  // Upload chunks sequentially
  for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
    const start = chunkIndex * CHUNK_SIZE;
    const end = Math.min(start + CHUNK_SIZE, file.size);
    const chunk = file.slice(start, end);
    
    const chunkProgress = Math.round((chunkIndex / totalChunks) * 90); // Reserve 10% for finalization
    updateProgress({
      status: 'uploading',
      message: `Uploading chunk ${chunkIndex + 1} of ${totalChunks}...`,
      loaded: start,
      percentage: chunkProgress
    });

    const response = await uploadChunk(
      chunk,
      uploadId,
      chunkIndex,
      totalChunks,
      file.name,
      medium,
      className,
      subject,
      token
    );

    if (!response.ok) {
      const error = await parseErrorResponse(response);
      updateProgress({ status: 'error', message: `Chunk ${chunkIndex + 1} failed: ${error.message}` });
      throw new ApiException(error);
    }
  }

  // Finalize upload
  updateProgress({
    status: 'processing',
    message: 'Assembling file on server...',
    loaded: file.size,
    percentage: 95
  });

  const finalizeResponse = await finalizeChunkedUpload(
    uploadId,
    file.name,
    totalChunks,
    file.size,
    medium,
    className,
    subject,
    token
  );

  if (!finalizeResponse.ok) {
    const error = await parseErrorResponse(finalizeResponse);
    updateProgress({ status: 'error', message: `Finalization failed: ${error.message}` });
    throw new ApiException(error);
  }

  const result = await finalizeResponse.json();
  
  updateProgress({
    status: 'completed',
    message: 'Upload complete!',
    loaded: file.size,
    percentage: 100
  });

  return result;
}

export async function uploadFile(file: File, medium: string, className: string, subject: string, token?: string): Promise<any> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('medium', medium);
  formData.append('className', className);
  formData.append('subject', subject);

  try {
    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(buildApiUrl('/api/file/upload'), {
      method: 'POST',
      headers: headers,
      body: formData
    });

    if (!response.ok) {
      const error = await parseErrorResponse(response);
      throw new ApiException(error);
    }

    return await response.json();
  } catch (error) {
    console.error('Upload error:', error);
    throw error;
  }
}

export async function getFaqs(token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl('/api/faqs'),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function getAnalytics(schoolId: string, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/analytics?schoolId=${schoolId}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(buildApiUrl('/health'), {
      method: 'GET'
    });
    return response.ok;
  } catch {
    return false;
  }
}

// ==========================================
// STUDY NOTES API
// ==========================================

export interface StudyNoteRequest {
  topic: string;
  subject?: string;
  grade?: string;
  chapter?: string;
}

export interface StudyNote {
  id: number;
  topic: string;
  notes: string;
  subject?: string;
  grade?: string;
  chapter?: string;
  createdAt: string;
  updatedAt?: string;
  rating?: number;
  isShared?: boolean;
  shareToken?: string;
}

export async function generateStudyNote(request: StudyNoteRequest, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl('/api/notes/generate'),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({
        Topic: request.topic,
        Subject: request.subject,
        Grade: request.grade,
        Chapter: request.chapter
      })
    },
    { maxRetries: 1 } // AI generation can take time
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function getUserStudyNotes(limit: number = 20, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes?limit=${limit}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function getStudyNoteById(id: number, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/${id}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function updateStudyNote(id: number, content: string, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/${id}`),
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ Content: content })
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function shareStudyNote(id: number, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/${id}/share`),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function unshareStudyNote(id: number, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/${id}/unshare`),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function getSharedStudyNote(shareToken: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/shared/${shareToken}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

export async function rateStudyNote(id: number, rating: number, token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/notes/${id}/rate`),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ Rating: rating })
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

// ==========================================
// CHAT HISTORY API (New Feature)
// ==========================================

export async function getMostRecentSession(token?: string): Promise<any> {
  const response = await fetchWithRetry(
    buildApiUrl('/api/chat/most-recent-session'),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

// ==========================================
// WRITTEN EXAM SUBMISSION API
// ==========================================

// File validation constants (must match backend)
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.pdf'];
const MAX_FILE_SIZE_MB = 10;
const MAX_FILES_PER_UPLOAD = 20;

export interface WrittenSubmissionResponse {
  writtenSubmissionId: string;
  status: string;
  message: string;
}

export interface SubmissionStatusResponse {
  writtenSubmissionId: string;
  status: 'PendingEvaluation' | 'OcrProcessing' | 'Evaluating' | 'Completed' | 'Failed';
  statusMessage: string;
  isComplete: boolean;
}

export interface WrittenExamResult {
  examId: string;
  studentId: string;
  mcqScore: number;
  mcqTotalMarks: number;
  subjectiveScore: number;
  subjectiveTotalMarks: number;
  grandScore: number;
  grandTotalMarks: number;
  percentage: number;
  grade: string;
  passed: boolean;
  subjectiveResults: Array<{
    questionId: string;
    earnedMarks: number;
    maxMarks: number;
    expectedAnswer: string;
    stepAnalysis: any[];
    overallFeedback: string;
  }>;
}

/**
 * Validates files before upload
 */
export function validateFiles(files: File[]): { valid: boolean; error?: string } {
  if (files.length === 0) {
    return { valid: false, error: 'Please select at least one file.' };
  }
  
  if (files.length > MAX_FILES_PER_UPLOAD) {
    return { valid: false, error: `Maximum ${MAX_FILES_PER_UPLOAD} files allowed per upload.` };
  }

  for (const file of files) {
    // Check file extension
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      return { 
        valid: false, 
        error: `Invalid file type: ${file.name}. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}` 
      };
    }

    // Check file size
    if (file.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
      return { 
        valid: false, 
        error: `File too large: ${file.name}. Maximum size: ${MAX_FILE_SIZE_MB}MB` 
      };
    }
  }

  return { valid: true };
}

/**
 * Upload written answer sheets (async - returns immediately)
 * After upload, poll with pollSubmissionStatus for results
 */
export async function submitWrittenAnswers(
  examId: string,
  studentId: string,
  files: File[],
  token?: string
): Promise<WrittenSubmissionResponse> {
  // Validate files before upload
  const validation = validateFiles(files);
  if (!validation.valid) {
    throw new ApiException({
      message: validation.error || 'Invalid files',
      code: 'VALIDATION_ERROR',
      status: 400,
    });
  }

  const formData = new FormData();
  formData.append('examId', examId);
  formData.append('studentId', studentId);
  
  for (const file of files) {
    formData.append('files', file);
  }

  // Don't use retry for large file uploads - they can be slow
  const response = await fetch(buildApiUrl('/api/exam/upload-written'), {
    method: 'POST',
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: formData,
  });

  if (!response.ok) {
    // Handle specific error cases
    if (response.status === 409) {
      throw new ApiException({
        message: 'You have already submitted answers for this exam. Duplicate submissions are not allowed.',
        code: 'DUPLICATE_SUBMISSION',
        status: 409,
      });
    }
    
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

/**
 * Poll submission status (call every 3 seconds until isComplete=true)
 */
export async function pollSubmissionStatus(
  writtenSubmissionId: string,
  token?: string
): Promise<SubmissionStatusResponse> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/exam/submission-status/${writtenSubmissionId}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    },
    { maxRetries: 2, retryDelay: 1000 }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}

/**
 * Get complete exam results after written evaluation is complete
 */
export async function getExamResults(
  examId: string,
  studentId: string,
  token?: string
): Promise<WrittenExamResult> {
  const response = await fetchWithRetry(
    buildApiUrl(`/api/exam/result/${examId}/${studentId}`),
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    }
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
}
