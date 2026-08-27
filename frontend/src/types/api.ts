export interface Candidate {
    id: string;
    email: string;
    fullName: string;
    phone?: string | null;
    createdAt: string;
}

export interface AuthResponse {
    token: string;
    expiresAt: string;
    candidate: Candidate;
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string | null;
    errors?: unknown;
}

export interface Resume {
    id: string;
    candidateId: string;
    fileName: string;
    originalFileName: string;
    contentType: string;
    fileSizeBytes: number;
    versionNumber: number;
    isLatest: boolean;
    status: string;
    createdAt: string;
    updatedAt: string;
}

export interface AtsAnalysis {
    id: string;
    resumeId: string;
    candidateId: string;
    overallScore: number;
    categoryScores?: Record<string, number> | null;
    skillsIdentified?: string[] | null;
    keywordsIdentified?: string[] | null;
    missingKeywords?: string[] | null;
    missingSkills?: string[] | null;
    issues?: string[] | null;
    recommendations?: string[] | null;
    analyzedAt: string;
}

/* ============================================================
   Jobs
   ============================================================ */

export interface Job {
    id: string;
    title: string;
    description: string;
    company: string;
    location?: string | null;
    employmentType?: string | null;
    requiredSkills?: string[] | null;
    preferredSkills?: string[] | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface JobSearchResult {
    items: Job[];
    total: number;
    page: number;
    pageSize: number;
}

/* ============================================================
   Matches
   ============================================================ */

export interface MatchResult {
    id: string;
    candidateId: string;
    resumeId: string;
    jobId: string;
    jobTitle: string;
    company: string;
    matchScore: number;
    matchingSkills?: string[] | null;
    missingSkills?: string[] | null;
    matchingKeywords?: string[] | null;
    missingKeywords?: string[] | null;
    reasons?: string[] | null;
    createdAt: string;
    updatedAt: string;
}

/* ============================================================
   Chat
   ============================================================ */

export interface ChatSession {
    id: string;
    candidateId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    messageCount: number;
}

export interface ChatMessage {
    id: string;
    chatSessionId: string;
    sender: string;
    content: string;
    createdAt: string;
}

export interface ChatSessionDetail {
    id: string;
    candidateId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    messages: ChatMessage[];
}

export interface ResumeChatResponse {
    answer: string;
    conversation: ChatMessage[];
}

export interface FaceLoginResponse {
    message: string;
    token: string;
    expiresAt: string;
    candidate: Candidate;
}