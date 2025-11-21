// API utility for backend calls
// ASP.NET Core backend (not Azure Functions - no function key needed)
export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

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
  maxRetries: 3,
  retryDelay: 1000,
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
      const response = await fetch(url, {
        ...options,
        signal: AbortSignal.timeout(30000), // 30s timeout
      });

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

export async function uploadFile(file: File, token?: string): Promise<any> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetchWithRetry(
    buildApiUrl('/api/file/upload'),
    {
      method: 'POST',
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: formData
    },
    { maxRetries: 1 } // Files can be large, limit retries
  );

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiException(error);
  }

  return await response.json();
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
      method: 'GET',
      signal: AbortSignal.timeout(5000), // 5s timeout for health check
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
