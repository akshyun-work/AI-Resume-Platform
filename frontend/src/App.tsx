import React, { useEffect, useState } from 'react';

import {
    FileText,
    Upload,
    UserCircle,
    LogOut,
    BarChart3,
    ShieldCheck,
    Briefcase,
    Target,
    MessageCircle,
} from 'lucide-react';

import { Login } from './components/Login';
import { Register } from './components/Register';
import { ResumeUpload } from './components/ResumeUpload';
import { ATSScorecard } from './components/ATSScorecard';
import { Profile } from './components/Profile';
import { Jobs } from './components/Jobs';
import { Matches } from './components/Matches';
import { ResumeChat } from './components/ResumeChat';

import {
    apiRequest,
    clearAuth,
    getAuthToken,
} from './services/api';

import type {
    ApiResponse,
    AtsAnalysis,
    Candidate,
    Resume,
} from './types/api';

type Page =
    | 'dashboard'
    | 'upload'
    | 'results'
    | 'profile'
    | 'jobs'
    | 'matches'
    | 'chat';

function App() {
    const [authenticated, setAuthenticated] =
        useState(Boolean(getAuthToken()));

    const [showRegister, setShowRegister] =
        useState(false);

    const [page, setPage] =
        useState<Page>('dashboard');

    const [candidate, setCandidate] =
        useState<Candidate | null>(null);

    const [latestResume, setLatestResume] =
        useState<Resume | null>(null);

    const [analysis, setAnalysis] =
        useState<AtsAnalysis | null>(null);

    const [loading, setLoading] =
        useState(false);

    const [error, setError] =
        useState<string | null>(null);

    // ============================================================
    // Load candidate + resume + ATS data after authentication
    // ============================================================

    const loadCandidateData = async () => {
        if (!getAuthToken()) {
            return;
        }

        try {
            setLoading(true);
            setError(null);

            // ----------------------------------------------------
            // Candidate
            // ----------------------------------------------------

            try {
                const candidateResponse =
                    await apiRequest<ApiResponse<Candidate>>(
                        '/api/candidates/me'
                    );

                if (
                    candidateResponse.success &&
                    candidateResponse.data
                ) {
                    setCandidate(candidateResponse.data);
                }
            } catch {
                // Candidate endpoint failure should not prevent
                // the rest of the dashboard from loading.
            }

            // ----------------------------------------------------
            // Latest resume
            // ----------------------------------------------------

            const resumeResponse =
                await apiRequest<
                    ApiResponse<Resume | null>
                >('/api/resumes/latest');

            if (
                resumeResponse.success &&
                resumeResponse.data
            ) {
                const resume =
                    resumeResponse.data;

                setLatestResume(resume);

                // ------------------------------------------------
                // Latest ATS analysis
                // ------------------------------------------------

                try {
                    const analysisResponse =
                        await apiRequest<
                            ApiResponse<AtsAnalysis>
                        >(
                            `/api/ats/resume/${resume.id}`
                        );

                    if (
                        analysisResponse.success &&
                        analysisResponse.data
                    ) {
                        setAnalysis(
                            analysisResponse.data
                        );
                    } else {
                        setAnalysis(null);
                    }
                } catch {
                    // No ATS analysis is a valid state.
                    setAnalysis(null);
                }
            } else {
                setLatestResume(null);
                setAnalysis(null);
            }
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to load dashboard.'
            );
        } finally {
            setLoading(false);
        }
    };
    // ============================================================
    // Refresh latest ATS analysis
    // ============================================================

    const refreshLatestAnalysis = async () => {
        if (!latestResume?.id) {
            setAnalysis(null);
            return;
        }

        try {
            setLoading(true);
            setError(null);

            const analysisResponse =
                await apiRequest<ApiResponse<AtsAnalysis>>(
                    `/api/ats/resume/${latestResume.id}`
                );

            if (
                analysisResponse.success &&
                analysisResponse.data
            ) {
                setAnalysis(analysisResponse.data);
            } else {
                setAnalysis(null);
            }
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to refresh ATS analysis.'
            );
        } finally {
            setLoading(false);
        }
    };
    // ============================================================
    // Load data after authentication
    // ============================================================

    useEffect(() => {
        if (authenticated) {
            loadCandidateData();
        }
    }, [authenticated]);

    // ============================================================
    // Login
    // ============================================================

    const handleLogin = (
        loggedInCandidate: Candidate
    ) => {
        setCandidate(loggedInCandidate);
        setAuthenticated(true);
        setPage('dashboard');
    };

    // ============================================================
    // Logout
    // ============================================================

    const handleLogout = () => {
        clearAuth();

        setAuthenticated(false);
        setCandidate(null);
        setLatestResume(null);
        setAnalysis(null);

        setPage('dashboard');
    };

    // ============================================================
    // Resume upload success
    // ============================================================

    const handleResumeUploaded = async (
        resume: Resume,
        uploadedAnalysis: AtsAnalysis
    ) => {
        setLatestResume(resume);
        setAnalysis(uploadedAnalysis);
        setPage('results');
    };

    // ============================================================
    // Authentication screen
    // ============================================================

    if (!authenticated) {
        return showRegister ? (
            <Register
                onSuccess={handleLogin}
                onLogin={() =>
                    setShowRegister(false)
                }
            />
        ) : (
            <Login
                onSuccess={handleLogin}
                onRegister={() =>
                    setShowRegister(true)
                }
            />
        );
    }

    // ============================================================
    // Authenticated application
    // ============================================================

    return (
        <div className="min-h-screen bg-slate-950 text-slate-100">

            {/* ====================================================
                Header
            ==================================================== */}

            <header className="border-b border-slate-800 bg-slate-950/90 backdrop-blur">
                <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">

                    <button
                        onClick={() =>
                            setPage('dashboard')
                        }
                        className="text-left"
                    >
                        <h1 className="bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-xl font-extrabold text-transparent">
                            Resume Intelligence
                        </h1>

                        <p className="text-xs text-slate-500">
                            AI-powered resume & ATS platform
                        </p>
                    </button>

                    <div className="flex items-center gap-2">

                        <button
                            onClick={() =>
                                setPage('profile')
                            }
                            className="flex items-center gap-2 rounded-xl px-3 py-2 text-sm text-slate-300 transition hover:bg-slate-800 hover:text-white"
                        >
                            <UserCircle size={18} />

                            {candidate?.fullName ||
                                'Profile'}
                        </button>

                        <button
                            onClick={handleLogout}
                            className="flex items-center gap-2 rounded-xl border border-slate-700 px-3 py-2 text-sm text-slate-400 transition hover:bg-slate-800 hover:text-white"
                        >
                            <LogOut size={17} />

                            Logout
                        </button>

                    </div>
                </div>
            </header>

            {/* ====================================================
                Main
            ==================================================== */}

            <main className="mx-auto max-w-7xl px-6 py-10">

                {/* Error */}

                {error && (
                    <div className="mb-6 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                        {error}
                    </div>
                )}

                {/* Loading */}

                {loading && (
                    <div className="mb-6 rounded-xl border border-slate-800 bg-slate-900 p-4 text-sm text-slate-400">
                        Loading your data...
                    </div>
                )}

                {/* =================================================
                    Dashboard
                ================================================= */}

                {page === 'dashboard' && (
                    <div className="space-y-8">

                        <section>
                            <p className="text-sm text-indigo-400">
                                Welcome back
                            </p>

                            <h2 className="mt-1 text-3xl font-bold text-white">
                                {candidate?.fullName ||
                                    'Candidate'}
                            </h2>

                            <p className="mt-2 text-slate-400">
                                Manage your resumes,
                                jobs and AI-powered
                                career analysis.
                            </p>
                        </section>

                        <div className="grid gap-5 md:grid-cols-3">

                            {/* Upload */}

                            <button
                                onClick={() =>
                                    setPage('upload')
                                }
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-indigo-500/50"
                            >
                                <Upload
                                    className="mb-4 text-indigo-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    Upload Resume
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    Upload a new PDF
                                    resume for analysis.
                                </p>
                            </button>

                            {/* ATS */}

                            <button
                                onClick={async () => {
                                    await refreshLatestAnalysis();
                                    setPage('results');
                                }}
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-purple-500/50"
                            >
                                <BarChart3
                                    className="mb-4 text-purple-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    ATS Results
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    View your latest
                                    resume analysis.
                                </p>
                            </button>

                            {/* Jobs */}

                            <button
                                onClick={() =>
                                    setPage('jobs')
                                }
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-blue-500/50"
                            >
                                <Briefcase
                                    className="mb-4 text-blue-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    Jobs
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    Search available
                                    job opportunities.
                                </p>
                            </button>

                            {/* Matches */}

                            <button
                                onClick={() =>
                                    setPage('matches')
                                }
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-emerald-500/50"
                            >
                                <Target
                                    className="mb-4 text-emerald-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    Job Matches
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    View compatibility
                                    with available jobs.
                                </p>
                            </button>

                            {/* Chat */}

                            <button
                                onClick={() =>
                                    setPage('chat')
                                }
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-cyan-500/50"
                            >
                                <MessageCircle
                                    className="mb-4 text-cyan-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    Resume Chat
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    Ask the AI about
                                    your resume.
                                </p>
                            </button>

                            {/* Security */}

                            <button
                                onClick={() =>
                                    setPage('profile')
                                }
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 text-left transition hover:border-indigo-500/50"
                            >
                                <ShieldCheck
                                    className="mb-4 text-emerald-400"
                                    size={28}
                                />

                                <h3 className="font-bold text-white">
                                    Security & Face ID
                                </h3>

                                <p className="mt-2 text-sm text-slate-400">
                                    Manage optional
                                    biometric authentication.
                                </p>
                            </button>

                        </div>

                        {/* Latest Resume */}

                        {latestResume && (
                            <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">

                                <div className="flex items-center justify-between gap-4">

                                    <div className="flex items-center gap-3">

                                        <FileText
                                            className="text-indigo-400"
                                            size={22}
                                        />

                                        <div>
                                            <p className="text-xs uppercase tracking-wider text-slate-500">
                                                Latest Resume
                                            </p>

                                            <p className="font-semibold text-white">
                                                {
                                                    latestResume.originalFileName
                                                }
                                            </p>
                                        </div>

                                    </div>

                                    <div className="flex gap-2">

                                        <button
                                            onClick={async () => {
                                                await refreshLatestAnalysis();
                                                setPage('results');
                                            }}
                                            className="rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800"
                                        >
                                            ATS Results
                                        </button>

                                        <button
                                            onClick={() =>
                                                setPage('matches')
                                            }
                                            className="rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800"
                                        >
                                            Matches
                                        </button>

                                        <button
                                            onClick={() =>
                                                setPage('chat')
                                            }
                                            className="rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-500"
                                        >
                                            Chat
                                        </button>

                                    </div>

                                </div>

                            </section>
                        )}

                    </div>
                )}

                {/* =================================================
                    Upload
                ================================================= */}

                {page === 'upload' && (
                    <ResumeUpload
                        onSuccess={handleResumeUploaded}
                        onBack={() => setPage('dashboard')}
                    />
                )}

                {/* =================================================
                    ATS Results
                ================================================= */}

                {page === 'results' && (
                    analysis ? (
                        <ATSScorecard
                            score={
                                analysis.overallScore
                            }
                            breakdown={
                                analysis.categoryScores || {}
                            }
                            skills={
                                analysis.skillsIdentified || []
                            }
                            keywords={
                                analysis.keywordsIdentified || []
                            }
                            missingKeywords={
                                analysis.missingKeywords || []
                            }
                            missingSkills={
                                analysis.missingSkills || []
                            }
                            issues={
                                analysis.issues || []
                            }
                            recommendations={
                                analysis.recommendations || []
                            }
                            name={
                                candidate?.fullName
                            }
                            email={
                                candidate?.email
                            }
                            phone={
                                candidate?.phone
                            }
                            analyzedAt={analysis.analyzedAt}
                            onBack={() =>
                                setPage('dashboard')
                            }
                        />
                    ) : (
                        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-10 text-center">

                            <BarChart3
                                size={42}
                                className="mx-auto mb-4 text-indigo-400"
                            />

                            <h2 className="text-xl font-bold text-white">
                                No ATS analysis yet
                            </h2>

                            <p className="mx-auto mt-2 max-w-md text-sm text-slate-400">
                                Upload a resume and run the AI
                                analysis to generate your ATS score.
                            </p>

                            <button
                                onClick={() =>
                                    setPage('upload')
                                }
                                className="mt-6 rounded-xl bg-indigo-600 px-5 py-3 text-sm font-semibold text-white hover:bg-indigo-500"
                            >
                                Upload Resume
                            </button>

                        </div>
                    )
                )}

                {/* =================================================
                    Jobs
                ================================================= */}

                {page === 'jobs' && (
                <Jobs
                    resumeId={latestResume?.id}
                    onBack={() =>
                        setPage('dashboard')
                    }
                    onMatchCreated={() =>
                        setPage('matches')
                    }
                    />
                )}

                {/* =================================================
                    Matches
                ================================================= */}

                {page === 'matches' && (
                    <Matches
                        resumeId={
                            latestResume?.id
                        }
                        onBack={() =>
                            setPage('dashboard')
                        }
                    />
                )}

                {/* =================================================
                    Resume Chat
                ================================================= */}

                {page === 'chat' && (
                    <ResumeChat
                        resumeId={
                            latestResume?.id
                        }
                        onBack={() =>
                            setPage('dashboard')
                        }
                    />
                )}

                {/* =================================================
                    Profile
                ================================================= */}

                {page === 'profile' &&
                    candidate && (
                        <Profile
                            candidate={candidate}
                            onBack={() =>
                                setPage('dashboard')
                            }
                        />
                    )}

            </main>
        </div>
    );
}

export default App;