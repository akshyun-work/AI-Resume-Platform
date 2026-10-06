import React, { useEffect, useState } from 'react';

import {
    Briefcase,
    MapPin,
    Search,
    Loader2,
    AlertCircle,
    RefreshCw,
    Target,
    FileText,
    CheckCircle2,
    ArrowLeft,
} from 'lucide-react';

import { apiRequest } from '../services/api';
import type { ApiResponse, MatchResult } from '../types/api';

interface Job {
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

interface JobsResult {
    items: Job[];
    total: number;
    page: number;
    pageSize: number;
}

interface JobsProps {
    resumeId?: string;
    initialSearch?: string;
    onBack: () => void;
    onMatchCreated: () => void;
    onViewJob: (job: Job) => void;
}

interface JobAnalysisResponse {
    match_score?: number;
    score?: number;
    keyword_score?: number;
    semantic_score?: number;
    job_match?: {
        score?: number;
        matched_required?: string[];
        missing_required?: string[];
        matched_preferred?: string[];
        missing_preferred?: string[];
        matched_job_skills?: string[];
        missing_job_skills?: string[];
    };
    comparison?: {
        matched_required?: string[];
        missing_required?: string[];
        matched_preferred?: string[];
        missing_preferred?: string[];
        matched_job_skills?: string[];
        missing_job_skills?: string[];
    };
    gemini_analysis?: {
        match_summary?: string;
        why_you_match?: string[];
        resume_strengths?: Array<{ strength?: string; description?: string } | string>;
        resume_weaknesses?: Array<{ weakness?: string; description?: string } | string>;
        explanation_of_job_match?: {
            score?: number;
            summary?: string;
            details?: string;
        };
        most_important_missing_skills?: {
            critical?: string[];
            beneficial?: string[];
        };
        prioritized_improvement_actions?: Array<{ action?: string; description?: string; timeframe?: string } | string>;
        career_direction?: {
            immediate_target?: string;
            stronger_alignment?: string;
        };
        [key: string]: unknown;
    } | string;
    [key: string]: unknown;
}

export const Jobs: React.FC<JobsProps> = ({
    resumeId,
    initialSearch = '',
    onBack,
    onMatchCreated,
    onViewJob,
}) => {
    const [jobs, setJobs] = useState<Job[]>([]);

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState<string | null>(null);

    const [search, setSearch] =
        useState(initialSearch);

    const [location, setLocation] =
        useState('');

    const [page, setPage] =
        useState(1);

    const [total, setTotal] =
        useState(0);

    const pageSize = 10;

    const [matchingJobId, setMatchingJobId] =
        useState<string | null>(null);

    const [customDescription, setCustomDescription] =
        useState('');

    const [customAnalysis, setCustomAnalysis] =
        useState<JobAnalysisResponse | null>(null);

    const [customLoading, setCustomLoading] =
        useState(false);

    const [customError, setCustomError] =
        useState<string | null>(null);

    // ============================================================
    // Load jobs
    // ============================================================

    const loadJobs = async (
        requestedPage = page,
        searchQuery?: string
    ) => {
        try {
            setLoading(true);
            setError(null);

            const effectiveSearch = searchQuery !== undefined ? searchQuery : search;
            const params =
                new URLSearchParams();

            if (effectiveSearch.trim()) {
                params.set(
                    'Search',
                    effectiveSearch.trim()
                );
            }

            if (location.trim()) {
                params.set(
                    'Location',
                    location.trim()
                );
            }

            params.set(
                'IsActive',
                'true'
            );

            params.set(
                'Page',
                String(requestedPage)
            );

            params.set(
                'PageSize',
                String(pageSize)
            );

            const response =
                await apiRequest<
                    ApiResponse<JobsResult>
                >(
                    `/api/jobs?${params.toString()}`
                );

            if (
                !response.success ||
                !response.data
            ) {
                throw new Error(
                    response.message ||
                    'Unable to load jobs.'
                );
            }

            setJobs(
                response.data.items || []
            );

            setTotal(
                response.data.total || 0
            );

            setPage(
                response.data.page ||
                requestedPage
            );

        } catch (err) {
            console.error(err);

            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to load jobs.'
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        setSearch(initialSearch);
        loadJobs(1, initialSearch);
    }, [initialSearch]);

    const handleSearch = (
        event: React.FormEvent
    ) => {
        event.preventDefault();
        loadJobs(1);
    };

    // ============================================================
    // Generate and save AI match
    // ============================================================

    const analyzeJobMatch = async (
        job: Job
    ) => {
        if (!resumeId) {
            setError(
                'Upload a resume before analyzing job matches.'
            );
            return;
        }

        try {
            setMatchingJobId(job.id);
            setError(null);

            const response =
                await apiRequest<
                    ApiResponse<MatchResult>
                >(
                    '/api/matches',
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/json',
                        },
                        body: JSON.stringify({
                            resumeId,
                            jobId: job.id,
                        }),
                    }
                );

            if (
                !response.success ||
                !response.data
            ) {
                throw new Error(
                    response.message ||
                    'Unable to generate job match.'
                );
            }

            // Match is now persisted in DB.
            onMatchCreated();

        } catch (err) {
            console.error(err);

            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to generate job match.'
            );
        } finally {
            setMatchingJobId(null);
        }
    };

    // ============================================================
    // Analyze custom job description
    // ============================================================

    const analyzeCustomDescription =
        async () => {
            if (!resumeId) {
                setCustomError(
                    'Upload a resume first.'
                );
                return;
            }

            if (
                !customDescription.trim()
            ) {
                setCustomError(
                    'Paste a job description first.'
                );
                return;
            }

            try {
                setCustomLoading(true);
                setCustomError(null);
                setCustomAnalysis(null);

                const response =
                    await apiRequest<
                        ApiResponse<JobAnalysisResponse>
                    >(
                        `/api/resumes/${resumeId}/analyze`,
                        {
                            method: 'POST',
                            headers: {
                                'Content-Type':
                                    'application/json',
                            },
                            body: JSON.stringify({
                                jobDescription:
                                    customDescription.trim(),
                                jobData: null,
                            }),
                        }
                    );

                if (
                    !response.success ||
                    !response.data
                ) {
                    throw new Error(
                        response.message ||
                        'Unable to analyze job description.'
                    );
                }

                let result = response.data;
                if (typeof result === 'string') {
                    try {
                        result = JSON.parse(result);
                    } catch {
                        // ignore
                    }
                }

                setCustomAnalysis(result);

            } catch (err) {
                console.error(err);

                setCustomError(
                    err instanceof Error
                        ? err.message
                        : 'Unable to analyze job description.'
                );
            } finally {
                setCustomLoading(false);
            }
        };

    const totalPages =
        Math.max(
            1,
            Math.ceil(
                total / pageSize
            )
        );

    return (
        <div className="space-y-8">

            {/* ====================================================
                Header
            ==================================================== */}

            <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">

                <div>

                    <button
                        onClick={onBack}
                        className="mb-3 flex items-center gap-2 text-sm text-indigo-400 hover:text-indigo-300"
                    >
                        <ArrowLeft size={16} />
                        Back to Dashboard
                    </button>

                    <h2 className="text-3xl font-bold text-white">
                        Jobs
                    </h2>

                    <p className="mt-2 text-slate-400">
                        Search jobs, analyze your resume against
                        them, or paste a custom job description.
                    </p>

                </div>

                <button
                    onClick={() =>
                        loadJobs(page)
                    }
                    disabled={loading}
                    className="flex items-center justify-center gap-2 rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-50"
                >
                    <RefreshCw size={16} />
                    Refresh
                </button>

            </div>

            {/* ====================================================
                Resume status
            ==================================================== */}

            {!resumeId && (
                <div className="flex items-start gap-3 rounded-xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-300">
                    <AlertCircle
                        size={18}
                        className="shrink-0"
                    />

                    <span>
                        Upload a resume first to use
                        AI job matching.
                    </span>
                </div>
            )}

            {/* ====================================================
                Job Search
            ==================================================== */}

            <form
                onSubmit={handleSearch}
                className="grid gap-3 rounded-2xl border border-slate-800 bg-slate-900 p-5 md:grid-cols-[1fr_1fr_auto]"
            >

                <div className="relative">

                    <Search
                        size={18}
                        className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                    />

                    <input
                        value={search}
                        onChange={(e) =>
                            setSearch(
                                e.target.value
                            )
                        }
                        placeholder="Search title, company, skills..."
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                    />

                </div>

                <div className="relative">

                    <MapPin
                        size={18}
                        className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                    />

                    <input
                        value={location}
                        onChange={(e) =>
                            setLocation(
                                e.target.value
                            )
                        }
                        placeholder="Location"
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                    />

                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="rounded-xl bg-indigo-600 px-6 py-3 text-sm font-semibold text-white hover:bg-indigo-500 disabled:opacity-50"
                >
                    Search
                </button>

            </form>

            {/* ====================================================
                Custom Job Description
            ==================================================== */}

            <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">

                <div className="flex items-start gap-3">

                    <div className="rounded-xl bg-indigo-500/10 p-3">
                        <FileText
                            size={22}
                            className="text-indigo-400"
                        />
                    </div>

                    <div>
                        <h3 className="font-bold text-white">
                            Analyze a Job Description
                        </h3>

                        <p className="mt-1 text-sm text-slate-400">
                            Paste any job description to compare
                            it against your latest resume.
                        </p>
                    </div>

                </div>

                <textarea
                    value={customDescription}
                    onChange={(e) =>
                        setCustomDescription(
                            e.target.value
                        )
                    }
                    placeholder="Paste the complete job description here..."
                    rows={7}
                    className="mt-5 w-full resize-y rounded-xl border border-slate-700 bg-slate-950 p-4 text-sm leading-6 text-white outline-none focus:border-indigo-500"
                />

                {customError && (
                    <div className="mt-4 flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                        <AlertCircle
                            size={18}
                            className="shrink-0"
                        />

                        <span>
                            {customError}
                        </span>
                    </div>
                )}

                <button
                    type="button"
                    onClick={
                        analyzeCustomDescription
                    }
                    disabled={
                        customLoading ||
                        !resumeId
                    }
                    className="mt-4 flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-3 text-sm font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {customLoading ? (
                        <>
                            <Loader2
                                size={17}
                                className="animate-spin"
                            />
                            Analyzing...
                        </>
                    ) : (
                        <>
                            <Target size={17} />
                            Analyze Job Description
                        </>
                    )}
                </button>

                {/* Custom analysis result */}

                {customAnalysis && (() => {
                    const score =
                        customAnalysis.match_score ??
                        customAnalysis.score ??
                        customAnalysis.job_match?.score ??
                        0;

                    const matchedSkills =
                        customAnalysis.comparison?.matched_required ||
                        customAnalysis.comparison?.matched_job_skills ||
                        customAnalysis.job_match?.matched_required ||
                        customAnalysis.job_match?.matched_job_skills ||
                        [];

                    const missingSkills =
                        customAnalysis.comparison?.missing_required ||
                        customAnalysis.comparison?.missing_job_skills ||
                        customAnalysis.job_match?.missing_required ||
                        customAnalysis.job_match?.missing_job_skills ||
                        [];

                    const ga = customAnalysis.gemini_analysis;
                    const isGaString = typeof ga === 'string';
                    const gaObj = !isGaString && typeof ga === 'object' && ga !== null ? ga : null;
                    const summary = gaObj ? (gaObj.match_summary || gaObj.explanation_of_job_match?.summary) : null;
                    const strengths = gaObj
                        ? (gaObj.why_you_match || (Array.isArray(gaObj.resume_strengths) ? gaObj.resume_strengths.map(s => typeof s === 'string' ? s : `${s.strength ? s.strength + ': ' : ''}${s.description || ''}`) : []))
                        : [];
                    const actions = gaObj && Array.isArray(gaObj.prioritized_improvement_actions)
                        ? gaObj.prioritized_improvement_actions.map(a => typeof a === 'string' ? a : `${a.action ? a.action + ': ' : ''}${a.description || ''}`)
                        : [];
                    const immediateTarget = gaObj?.career_direction?.immediate_target;

                    return (
                        <div className="mt-6 rounded-xl border border-indigo-500/20 bg-slate-950 p-5">
                            <div className="flex items-center justify-between gap-4">
                                <div>
                                    <p className="text-xs uppercase tracking-wider text-slate-500">
                                        Job Match Score
                                    </p>
                                    <p className="mt-1 text-4xl font-black text-indigo-400">
                                        {score}
                                        <span className="text-sm text-slate-500">
                                            /100
                                        </span>
                                    </p>
                                </div>

                                <CheckCircle2
                                    size={28}
                                    className="text-emerald-400"
                                />
                            </div>

                            {(matchedSkills.length > 0 || missingSkills.length > 0) && (
                                <div className="mt-6 grid gap-5 md:grid-cols-2">
                                    <div>
                                        <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                            Matching Skills ({matchedSkills.length})
                                        </p>
                                        <div className="flex flex-wrap gap-2">
                                            {matchedSkills.length > 0 ? (
                                                matchedSkills.map((skill) => (
                                                    <span
                                                        key={skill}
                                                        className="rounded-lg bg-emerald-500/10 px-2 py-1 text-xs text-emerald-300"
                                                    >
                                                        {skill}
                                                    </span>
                                                ))
                                            ) : (
                                                <span className="text-xs text-slate-500 italic">None detected</span>
                                            )}
                                        </div>
                                    </div>

                                    <div>
                                        <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                            Missing Skills ({missingSkills.length})
                                        </p>
                                        <div className="flex flex-wrap gap-2">
                                            {missingSkills.length > 0 ? (
                                                missingSkills.map((skill) => (
                                                    <span
                                                        key={skill}
                                                        className="rounded-lg bg-rose-500/10 px-2 py-1 text-xs text-rose-300"
                                                    >
                                                        {skill}
                                                    </span>
                                                ))
                                            ) : (
                                                <span className="text-xs text-slate-500 italic">None missing</span>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            )}

                            {ga && (
                                <div className="mt-6 border-t border-slate-800/80 pt-5">
                                    <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-indigo-400">
                                        AI Match Analysis
                                    </p>
                                    {isGaString ? (
                                        <p className="whitespace-pre-wrap text-sm leading-6 text-slate-300">
                                            {ga}
                                        </p>
                                    ) : (
                                        <div className="space-y-4 text-sm leading-6 text-slate-300">
                                            {summary && (
                                                <p className="text-slate-300">{summary}</p>
                                            )}
                                            {strengths.length > 0 && (
                                                <div>
                                                    <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-emerald-400">
                                                        Key Strengths
                                                    </p>
                                                    <ul className="list-inside list-disc space-y-1 text-xs text-slate-300">
                                                        {strengths.map((str, idx) => (
                                                            <li key={idx}>{str}</li>
                                                        ))}
                                                    </ul>
                                                </div>
                                            )}
                                            {actions.length > 0 && (
                                                <div>
                                                    <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-amber-400">
                                                        Recommended Next Steps
                                                    </p>
                                                    <ul className="list-inside list-disc space-y-1 text-xs text-slate-300">
                                                        {actions.map((act, idx) => (
                                                            <li key={idx}>{act}</li>
                                                        ))}
                                                    </ul>
                                                </div>
                                            )}
                                            {immediateTarget && (
                                                <div className="rounded-lg border border-indigo-500/20 bg-indigo-950/40 p-3 text-xs text-indigo-200">
                                                    <strong>Career Target Advice: </strong>{immediateTarget}
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    );
                })()}

            </section>

            {/* ====================================================
                Error
            ==================================================== */}

            {error && (
                <div className="flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                    <AlertCircle
                        size={18}
                        className="shrink-0"
                    />

                    <span>{error}</span>
                </div>
            )}

            {/* ====================================================
                Job Results
            ==================================================== */}

            {loading ? (
                <div className="flex items-center justify-center rounded-2xl border border-slate-800 bg-slate-900 p-12">
                    <Loader2
                        size={30}
                        className="animate-spin text-indigo-400"
                    />
                </div>
            ) : jobs.length === 0 ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-12 text-center">

                    <Briefcase
                        size={42}
                        className="mx-auto mb-4 text-slate-600"
                    />

                    <h3 className="text-lg font-bold text-white">
                        No jobs found
                    </h3>

                    <p className="mt-2 text-sm text-slate-400">
                        {search ? `No jobs matched "${search}".` : 'No jobs available currently.'}
                    </p>

                    {search && (
                        <button
                            type="button"
                            onClick={() => {
                                setSearch('');
                                loadJobs(1, '');
                            }}
                            className="mt-5 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 text-xs font-semibold text-white transition hover:bg-indigo-500"
                        >
                            <RefreshCw size={14} />
                            View All Available Jobs
                        </button>
                    )}

                </div>
            ) : (
                <>
                    <div className="flex items-center justify-between">
                        <p className="text-sm text-slate-500">
                            Showing {jobs.length} of {total} jobs
                        </p>
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">

                        {jobs.map((job) => (
                            <div
                                key={job.id}
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 transition hover:border-indigo-500/40"
                            >

                                <div className="flex items-start gap-4">

                                    <div className="rounded-xl bg-indigo-500/10 p-3">
                                        <Briefcase
                                            size={24}
                                            className="text-indigo-400"
                                        />
                                    </div>

                                    <div className="min-w-0">

                                        <h3 className="font-bold text-white">
                                            {job.title}
                                        </h3>

                                        <p className="mt-1 text-sm text-indigo-400">
                                            {job.company}
                                        </p>

                                    </div>

                                </div>

                                <div className="mt-5 flex flex-wrap gap-3 text-xs text-slate-400">

                                    {job.location && (
                                        <span className="flex items-center gap-1">
                                            <MapPin size={14} />
                                            {job.location}
                                        </span>
                                    )}

                                    {job.employmentType && (
                                        <span className="rounded-lg bg-slate-800 px-2 py-1">
                                            {job.employmentType}
                                        </span>
                                    )}

                                </div>

                                <p className="mt-5 line-clamp-4 text-sm leading-6 text-slate-400">
                                    {job.description}
                                </p>

                                {job.requiredSkills &&
                                    job.requiredSkills.length > 0 && (
                                        <div className="mt-5">

                                            <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                                Required Skills
                                            </p>

                                            <div className="flex flex-wrap gap-2">
                                                {job.requiredSkills.map(
                                                    (skill) => (
                                                        <span
                                                            key={skill}
                                                            className="rounded-lg border border-indigo-500/20 bg-indigo-500/10 px-2 py-1 text-xs text-indigo-300"
                                                        >
                                                            {skill}
                                                        </span>
                                                    )
                                                )}
                                            </div>

                                        </div>
                                    )}

                                <div className="mt-6 flex gap-3">
                                    <button
                                        type="button"
                                        onClick={() => onViewJob(job)}
                                        className="flex-1 rounded-xl border border-slate-700 px-4 py-3 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 hover:text-white"
                                    >
                                        View Job Details
                                    </button>

                                    <button
                                        type="button"
                                        onClick={() =>
                                            analyzeJobMatch(
                                                job
                                            )
                                        }
                                        disabled={
                                            !resumeId ||
                                            matchingJobId === job.id
                                        }
                                        className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
                                    >
                                        {matchingJobId ===
                                            job.id ? (
                                            <>
                                                <Loader2
                                                    size={16}
                                                    className="animate-spin"
                                                />
                                                Matching...
                                            </>
                                        ) : (
                                            <>
                                                <Target size={16} />
                                                Analyze Match
                                            </>
                                        )}
                                    </button>
                                </div>

                            </div>
                        ))}

                    </div>

                    {/* Pagination */}

                    {totalPages > 1 && (
                        <div className="flex items-center justify-center gap-3">

                            <button
                                type="button"
                                disabled={page <= 1 || loading}
                                onClick={() =>
                                    loadJobs(
                                        page - 1
                                    )
                                }
                                className="rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-40"
                            >
                                Previous
                            </button>

                            <span className="text-sm text-slate-500">
                                Page {page} of {totalPages}
                            </span>

                            <button
                                type="button"
                                disabled={
                                    page >= totalPages ||
                                    loading
                                }
                                onClick={() =>
                                    loadJobs(
                                        page + 1
                                    )
                                }
                                className="rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-40"
                            >
                                Next
                            </button>

                        </div>
                    )}

                </>
            )}

        </div>
    );
};