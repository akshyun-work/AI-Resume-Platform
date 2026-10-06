import React from 'react';
import {
    Award,
    Code,
    GraduationCap,
    Mail,
    Phone,
    CheckCircle2,
    AlertTriangle,
    Search,
    AlertCircle,
    Lightbulb,
    ArrowLeft,
    Target,
    Briefcase,
    Sparkles,
    Check,
    Compass,
    Eye,
    FileText,
    ExternalLink,
    Building2,
    MapPin,
} from 'lucide-react';

import { apiRequest } from '../services/api';
import type { ApiResponse, Job } from '../types/api';

interface ATSScorecardProps {
    score: number;
    breakdown?: Record<string, number> | null;
    skills: string[];
    keywords: string[];
    missingKeywords: string[];
    missingSkills: string[];
    issues: string[];
    recommendations: string[];
    name?: string | null;
    email?: string | null;
    phone?: string | null;
    analyzedAt?: string;
    targetJobTitle?: string | null;
    targetCompany?: string | null;
    targetJobId?: string | null;
    onBack: () => void;
    onSelectRole?: (role: string) => void;
    onViewJob?: (job: Job) => void;
    onViewJobById?: (jobId: string) => void;
    onViewResume?: () => void;
}

interface ParsedIssue {
    title?: string;
    description: string;
    type?: 'warning' | 'info' | 'error';
}

interface ParsedRecommendation {
    title?: string;
    description: string;
}

/**
 * Parses and sanitizes issues, ensuring raw JSON strings from legacy
 * match reasons are converted into clean human-readable points.
 */
const parseIssues = (rawIssues: string[]): ParsedIssue[] => {
    const results: ParsedIssue[] = [];

    if (!rawIssues || rawIssues.length === 0) return results;

    for (const raw of rawIssues) {
        if (!raw || typeof raw !== 'string') continue;
        const trimmed = raw.trim();

        // Check if string contains or is a JSON blob
        if (trimmed.startsWith('{') && trimmed.endsWith('}')) {
            try {
                const parsed = JSON.parse(trimmed);
                if (parsed && typeof parsed === 'object') {
                    // Extract from resume_weaknesses
                    if (Array.isArray(parsed.resume_weaknesses)) {
                        for (const w of parsed.resume_weaknesses) {
                            if (typeof w === 'string') {
                                results.push({ description: w, type: 'warning' });
                            } else if (w && typeof w === 'object') {
                                results.push({
                                    title: w.title,
                                    description: w.description || w.title,
                                    type: 'warning',
                                });
                            }
                        }
                    }

                    // Extract from factors
                    if (Array.isArray(parsed.factors)) {
                        for (const f of parsed.factors) {
                            if (typeof f === 'string' && f.trim()) {
                                results.push({ description: f, type: 'warning' });
                            }
                        }
                    }

                    // Extract from what_is_missing
                    const missingReq =
                        parsed.what_is_missing?.required ??
                        parsed.most_important_missing_skills?.required;
                    if (Array.isArray(missingReq) && missingReq.length > 0) {
                        results.push({
                            title: 'Missing Required Skills',
                            description: `Skills not found on resume: ${missingReq.join(', ')}`,
                            type: 'warning',
                        });
                    }

                    const missingPref =
                        parsed.what_is_missing?.preferred ??
                        parsed.most_important_missing_skills?.preferred;
                    if (Array.isArray(missingPref) && missingPref.length > 0) {
                        results.push({
                            title: 'Missing Preferred Skills',
                            description: `Recommended additions: ${missingPref.join(', ')}`,
                            type: 'info',
                        });
                    }
                    continue;
                }
            } catch {
                // Not valid JSON, continue to string cleaner
            }
        }

        // Regular issue string
        const cleanStr = trimmed.replace(/^[\*\-\•\d\.\s]+/, '').trim();
        if (cleanStr) {
            const colonIdx = cleanStr.indexOf(':');
            if (colonIdx > 0 && colonIdx < 40) {
                results.push({
                    title: cleanStr.slice(0, colonIdx).trim(),
                    description: cleanStr.slice(colonIdx + 1).trim(),
                    type: cleanStr.toLowerCase().includes('missing') ? 'warning' : 'info',
                });
            } else {
                results.push({
                    description: cleanStr,
                    type: 'info',
                });
            }
        }
    }

    return results;
};

/**
 * Parses and ensures rich, actionable recommendations.
 */
const parseRecommendations = (
    rawRecs: string[],
    skills: string[],
    targetJobTitle?: string | null
): ParsedRecommendation[] => {
    const results: ParsedRecommendation[] = [];

    if (rawRecs && rawRecs.length > 0) {
        for (const raw of rawRecs) {
            if (!raw || typeof raw !== 'string') continue;
            const trimmed = raw.trim();

            // If JSON
            if (trimmed.startsWith('{') && trimmed.endsWith('}')) {
                try {
                    const parsed = JSON.parse(trimmed);
                    const actions =
                        parsed.improvement_actions ??
                        parsed.prioritized_improvement_actions;
                    if (Array.isArray(actions)) {
                        for (const a of actions) {
                            if (typeof a === 'string') {
                                results.push({ description: a });
                            } else if (a && typeof a === 'object') {
                                results.push({
                                    title: a.title,
                                    description: a.action || a.description,
                                });
                            }
                        }
                    }
                    continue;
                } catch {
                    // Ignore malformed JSON
                }
            }

            const clean = trimmed.replace(/^[\*\-\•\d\.\s]+/, '').trim();
            if (clean) {
                const colonIdx = clean.indexOf(':');
                if (colonIdx > 0 && colonIdx < 40) {
                    results.push({
                        title: clean.slice(0, colonIdx).trim(),
                        description: clean.slice(colonIdx + 1).trim(),
                    });
                } else {
                    results.push({ description: clean });
                }
            }
        }
    }

    // Default intelligent recommendations if none provided
    if (results.length === 0) {
        if (targetJobTitle) {
            results.push({
                title: `Tailor Resume for ${targetJobTitle}`,
                description: `Align your project bullet points and tech stack keywords directly with the expectations for ${targetJobTitle}.`,
            });
        }
        results.push({
            title: 'Quantify Engineering Impact',
            description:
                'Add measurable business metrics (e.g. latency reduced by 30%, handled 5,000+ daily requests) to your project bullet points.',
        });
        results.push({
            title: 'Showcase Live Project Links & Certifications',
            description:
                'Ensure GitHub repositories, live demo URLs, and relevant technical credentials are explicitly linked.',
        });
    }

    return results;
};

const getScoreLabel = (score: number) => {
    if (score >= 80) return 'ATS Optimized';
    if (score >= 60) return 'Needs Improvement';
    return 'High Rejection Risk';
};

const getScoreClass = (score: number) => {
    if (score >= 80) {
        return 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10';
    }
    if (score >= 60) {
        return 'text-amber-400 border-amber-500/30 bg-amber-500/10';
    }
    return 'text-rose-400 border-rose-500/30 bg-rose-500/10';
};

const formatLabel = (value: string) =>
    value
        .replace(/_/g, ' ')
        .replace(/\b\w/g, (char) => char.toUpperCase());

export const ATSScorecard: React.FC<ATSScorecardProps> = ({
    score,
    breakdown,
    skills,
    keywords,
    missingKeywords,
    missingSkills,
    issues,
    recommendations,
    name,
    email,
    phone,
    analyzedAt,
    targetJobTitle,
    targetCompany,
    targetJobId,
    onBack,
    onSelectRole,
    onViewJob,
    onViewJobById,
    onViewResume,
}) => {
    const categories = breakdown ? Object.entries(breakdown) : [];
    const parsedIssuesList = parseIssues(issues);
    const parsedRecsList = parseRecommendations(recommendations, skills, targetJobTitle);

    // Dynamic recommended catalog jobs
    const [matchingCatalogJobs, setMatchingCatalogJobs] = React.useState<Job[]>([]);
    const [loadingCatalogJobs, setLoadingCatalogJobs] = React.useState(false);

    React.useEffect(() => {
        let isMounted = true;
        const fetchCatalogJobs = async () => {
            try {
                setLoadingCatalogJobs(true);
                const res = await apiRequest<ApiResponse<{ items: Job[] }>>('/api/jobs?IsActive=true&PageSize=10');
                if (isMounted && res.success && res.data?.items) {
                    const sLower = skills.map((s) => s.toLowerCase());
                    const scored = res.data.items.map((j) => {
                        const allJobSkills = [
                            ...(j.requiredSkills || []),
                            ...(j.preferredSkills || []),
                            ...(j.structured?.requiredSkills || []),
                            ...(j.structured?.preferredSkills || []),
                        ].map((s) => s.toLowerCase());

                        const matchCount = allJobSkills.filter((js) =>
                            sLower.some((s) => s.includes(js) || js.includes(s))
                        ).length;

                        return { job: j, matchCount };
                    });

                    scored.sort((a, b) => b.matchCount - a.matchCount);
                    setMatchingCatalogJobs(scored.map((s) => s.job).slice(0, 4));
                }
            } catch {
                // Ignore failure
            } finally {
                if (isMounted) setLoadingCatalogJobs(false);
            }
        };

        fetchCatalogJobs();
        return () => {
            isMounted = false;
        };
    }, [skills]);

    // If keywords is empty initially, display relevant skills as identified keywords
    const displayKeywords = keywords && keywords.length > 0 ? keywords : skills;

    return (
        <div className="w-full max-w-5xl space-y-6">
            {/* Back Button */}
            <button
                type="button"
                onClick={onBack}
                className="flex items-center gap-2 text-sm font-medium text-indigo-400 transition hover:text-indigo-300"
            >
                <ArrowLeft size={16} />
                Back to Dashboard
            </button>

            {/* Candidate Info Header */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                        <p className="text-xs uppercase tracking-wider text-slate-500">
                            Candidate Profile
                        </p>
                        <h2 className="mt-1 text-xl font-bold text-white">
                            {name || 'Candidate'}
                        </h2>

                        {targetJobTitle && (
                            <div className="mt-2.5 flex flex-wrap items-center gap-2">
                                <div className="inline-flex items-center gap-2 rounded-xl border border-indigo-500/30 bg-indigo-500/10 px-3 py-1.5 text-xs text-indigo-300">
                                    <Target size={14} className="text-indigo-400 shrink-0" />
                                    <span>
                                        Target Position: <strong className="text-white">{targetJobTitle}</strong>
                                        {targetCompany && <span className="text-slate-300"> ({targetCompany})</span>}
                                    </span>
                                </div>

                                {targetJobId && (onViewJobById || onViewJob) && (
                                    <button
                                        type="button"
                                        onClick={() => onViewJobById?.(targetJobId)}
                                        className="inline-flex items-center gap-1.5 rounded-xl border border-indigo-500/40 bg-indigo-600/25 px-3 py-1.5 text-xs font-semibold text-indigo-200 transition hover:bg-indigo-600 hover:text-white"
                                    >
                                        <Eye size={13} />
                                        View Target Job
                                    </button>
                                )}
                            </div>
                        )}
                    </div>

                    <div className="flex flex-wrap items-center gap-4 text-xs text-slate-400">
                        {email && (
                            <span className="flex items-center gap-2">
                                <Mail size={14} className="text-indigo-400" />
                                {email}
                            </span>
                        )}

                        {phone && (
                            <span className="flex items-center gap-2">
                                <Phone size={14} className="text-indigo-400" />
                                {phone}
                            </span>
                        )}

                        {onViewResume && (
                            <button
                                type="button"
                                onClick={onViewResume}
                                className="flex items-center gap-1.5 rounded-xl border border-indigo-500/30 bg-indigo-600/15 px-3.5 py-2 text-xs font-semibold text-indigo-300 transition hover:bg-indigo-600 hover:text-white"
                            >
                                <Eye size={14} />
                                View Resume PDF
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Score + Breakdown */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
                {/* Score Dial */}
                <div className="flex flex-col items-center justify-center rounded-2xl border border-slate-800 bg-slate-900 p-8 text-center">
                    <div
                        className={`flex h-40 w-40 flex-col items-center justify-center rounded-full border-4 ${getScoreClass(
                            score
                        )}`}
                    >
                        <span className="text-5xl font-black">{score}</span>
                        <span className="text-xs font-semibold uppercase tracking-wider opacity-70">
                            / 100
                        </span>
                    </div>

                    <h3 className="mt-5 flex items-center gap-2 font-semibold text-white">
                        {score >= 80 ? (
                            <CheckCircle2 size={18} className="text-emerald-400" />
                        ) : (
                            <AlertTriangle size={18} className="text-amber-400" />
                        )}
                        {getScoreLabel(score)}
                    </h3>

                    {analyzedAt && (
                        <p className="mt-2 text-xs text-slate-500">
                            Analyzed {new Date(analyzedAt).toLocaleString()}
                        </p>
                    )}
                </div>

                {/* Category Breakdown */}
                <div className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900 p-6 lg:col-span-2">
                    <div>
                        <h3 className="text-sm font-semibold text-white">
                            ATS Category Breakdown
                        </h3>
                        <p className="mt-1 text-xs text-slate-500">
                            Evaluation of resume format, technical competency, and section completeness.
                        </p>
                    </div>

                    {categories.length === 0 && (
                        <p className="text-sm text-slate-500">
                            No category breakdown was returned.
                        </p>
                    )}

                    {categories.map(([category, value]) => (
                        <div key={category}>
                            <div className="mb-1 flex justify-between text-xs">
                                <span className="text-slate-300">
                                    {formatLabel(category)}
                                </span>
                                <span className="font-semibold text-slate-400">
                                    {value}
                                </span>
                            </div>

                            <div className="h-2 overflow-hidden rounded-full bg-slate-800">
                                <div
                                    className="h-full rounded-full bg-indigo-500 transition-all duration-500"
                                    style={{
                                        width: `${Math.min(Math.max(value, 0), 100)}%`,
                                    }}
                                />
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* 4-Grid Information Panel */}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                {/* 1. Skills Identified */}
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                        <Code size={17} className="text-indigo-400" />
                        Skills Identified
                    </h3>
                    {skills.length === 0 ? (
                        <p className="mt-4 text-xs text-slate-500">
                            No technical skills detected.
                        </p>
                    ) : (
                        <div className="mt-4 flex flex-wrap gap-2">
                            {skills.map((item, index) => (
                                <span
                                    key={`${item}-${index}`}
                                    className="rounded-lg border border-indigo-500/20 bg-indigo-500/10 px-3 py-1.5 text-xs font-medium text-indigo-300"
                                >
                                    {item}
                                </span>
                            ))}
                        </div>
                    )}
                </div>

                {/* 2. Keywords Identified */}
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                        <Search size={17} className="text-slate-400" />
                        Keywords Identified
                    </h3>
                    {displayKeywords.length === 0 ? (
                        <p className="mt-4 text-xs text-slate-500">
                            No keywords identified yet.
                        </p>
                    ) : (
                        <div className="mt-4 flex flex-wrap gap-2">
                            {displayKeywords.map((item, index) => (
                                <span
                                    key={`${item}-${index}`}
                                    className="rounded-lg border border-slate-700 bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-300"
                                >
                                    {item}
                                </span>
                            ))}
                        </div>
                    )}
                </div>

                {/* 3. Missing Skills */}
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                        <GraduationCap size={17} className="text-amber-400" />
                        Missing Skills {targetJobTitle ? `(${targetJobTitle})` : ''}
                    </h3>
                    {missingSkills && missingSkills.length > 0 ? (
                        <div className="mt-4 flex flex-wrap gap-2">
                            {missingSkills.map((item, index) => (
                                <span
                                    key={`${item}-${index}`}
                                    className="rounded-lg border border-amber-500/20 bg-amber-500/10 px-3 py-1.5 text-xs font-medium text-amber-300"
                                >
                                    {item}
                                </span>
                            ))}
                        </div>
                    ) : (
                        <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 p-3.5 text-xs text-slate-400 flex items-start gap-2.5">
                            <Target size={16} className="text-indigo-400 shrink-0 mt-0.5" />
                            <span>
                                Select a target job in the <strong className="text-slate-300">Jobs</strong> tab to compare and identify role-specific missing skills.
                            </span>
                        </div>
                    )}
                </div>

                {/* 4. Missing Keywords */}
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                        <Search size={17} className="text-rose-400" />
                        Missing Keywords {targetJobTitle ? `(${targetJobTitle})` : ''}
                    </h3>
                    {missingKeywords && missingKeywords.length > 0 ? (
                        <div className="mt-4 flex flex-wrap gap-2">
                            {missingKeywords.map((item, index) => (
                                <span
                                    key={`${item}-${index}`}
                                    className="rounded-lg border border-rose-500/20 bg-rose-500/10 px-3 py-1.5 text-xs font-medium text-rose-300"
                                >
                                    {item}
                                </span>
                            ))}
                        </div>
                    ) : (
                        <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 p-3.5 text-xs text-slate-400 flex items-start gap-2.5">
                            <Target size={16} className="text-indigo-400 shrink-0 mt-0.5" />
                            <span>
                                Select a target job in the <strong className="text-slate-300">Jobs</strong> tab to identify missing industry-specific ATS keywords.
                            </span>
                        </div>
                    )}
                </div>
            </div>

            {/* Restructured Issues Panel */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                    <AlertCircle size={17} className="text-amber-400" />
                    Issues & Areas for Improvement
                </h3>

                {parsedIssuesList.length === 0 ? (
                    <div className="mt-4 flex items-center gap-3 rounded-xl border border-emerald-500/20 bg-emerald-500/10 p-4 text-xs text-emerald-300">
                        <Check size={16} className="shrink-0 text-emerald-400" />
                        <span>
                            No format or ATS parsing issues detected. Your resume layout, contact information, and section structure are properly formatted.
                        </span>
                    </div>
                ) : (
                    <div className="mt-4 space-y-3">
                        {parsedIssuesList.map((issue, index) => (
                            <div
                                key={index}
                                className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-4 text-sm text-slate-300"
                            >
                                <AlertTriangle
                                    size={16}
                                    className="mt-0.5 shrink-0 text-amber-400"
                                />
                                <div>
                                    {issue.title && (
                                        <strong className="block text-white mb-0.5 font-medium">
                                            {issue.title}
                                        </strong>
                                    )}
                                    <span className="text-slate-300 text-xs leading-relaxed">
                                        {issue.description}
                                    </span>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Recommendations & Suggested Directions */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                {/* Actionable Recommendations */}
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
                        <Lightbulb size={17} className="text-amber-400" />
                        Optimization Recommendations
                    </h3>

                    <div className="mt-4 space-y-3">
                        {parsedRecsList.map((rec, index) => (
                            <div
                                key={index}
                                className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-3.5 text-xs text-slate-300"
                            >
                                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-indigo-500/20 text-xs font-bold text-indigo-400">
                                    {index + 1}
                                </span>
                                <div>
                                    {rec.title && (
                                        <strong className="block text-white mb-0.5">
                                            {rec.title}
                                        </strong>
                                    )}
                                    <span className="text-slate-400 leading-relaxed">
                                        {rec.description}
                                    </span>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Recommended Jobs */}
                <div className="rounded-2xl border border-indigo-900/30 bg-indigo-950/10 p-6 space-y-4">
                    <div>
                        <h3 className="flex items-center gap-2 text-sm font-semibold text-indigo-300">
                            <Briefcase size={17} className="text-indigo-400" />
                            Recommended Jobs For You
                        </h3>
                        <p className="mt-1 text-xs text-slate-400">
                            Top matching positions on the platform aligned with your identified technical stack.
                        </p>
                    </div>

                    {/* Matching Jobs in Platform */}
                    {loadingCatalogJobs ? (
                        <div className="flex items-center justify-center p-8 text-xs text-indigo-400">
                            Loading matching jobs...
                        </div>
                    ) : matchingCatalogJobs.length > 0 ? (
                        <div className="space-y-3">
                            {matchingCatalogJobs.map((job) => (
                                <div
                                    key={job.id}
                                    onClick={() => (onViewJob ? onViewJob(job) : onViewJobById?.(job.id))}
                                    className="flex flex-col gap-2 rounded-xl border border-slate-800 bg-slate-900/90 p-4 text-xs transition hover:border-indigo-500/50 hover:bg-slate-850 cursor-pointer group"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div>
                                            <h4 className="font-bold text-white group-hover:text-indigo-200 text-sm">
                                                {job.title}
                                            </h4>
                                            <p className="mt-1 text-indigo-400 font-medium flex items-center gap-1.5">
                                                <Building2 size={13} />
                                                {job.company}
                                                {job.location && (
                                                    <span className="text-slate-400 flex items-center gap-1 ml-1.5">
                                                        <MapPin size={12} />
                                                        {job.location}
                                                    </span>
                                                )}
                                            </p>
                                        </div>

                                        <button
                                            type="button"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                if (onViewJob) onViewJob(job);
                                                else if (onViewJobById) onViewJobById(job.id);
                                            }}
                                            className="shrink-0 rounded-lg bg-indigo-600/20 px-3 py-1.5 text-xs font-semibold text-indigo-300 border border-indigo-500/30 group-hover:bg-indigo-600 group-hover:text-white transition flex items-center gap-1.5"
                                        >
                                            <Eye size={13} />
                                            View Job &rarr;
                                        </button>
                                    </div>

                                    {(job.requiredSkills || job.structured?.requiredSkills) && (
                                        <div className="flex flex-wrap gap-1.5 mt-1.5">
                                            {(job.requiredSkills || job.structured?.requiredSkills || [])
                                                .slice(0, 4)
                                                .map((sk, skIdx) => (
                                                    <span
                                                        key={skIdx}
                                                        className="rounded-md bg-slate-800/80 px-2 py-0.5 text-[11px] text-slate-300"
                                                    >
                                                        {sk}
                                                    </span>
                                                ))}
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-4 text-xs text-slate-400">
                            No active catalog jobs found matching your skill profile currently.
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};
