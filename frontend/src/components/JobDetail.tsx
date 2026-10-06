import React, { useState, useEffect } from 'react';
import {
    ArrowLeft,
    Briefcase,
    MapPin,
    Building2,
    Clock,
    Target,
    Loader2,
    Sparkles,
    CheckCircle2,
    GraduationCap,
    Gift,
    Mail,
    ExternalLink,
    ChevronDown,
    ChevronUp,
    FileText,
    Copy,
    Check,
    Layers,
} from 'lucide-react';
import type { Job, ApiResponse } from '../types/api';
import { apiRequest } from '../services/api';

interface JobDetailProps {
    job: Job;
    resumeId?: string;
    onBack: () => void;
    onAnalyzeMatch: (job: Job) => Promise<void>;
}

export const JobDetail: React.FC<JobDetailProps> = ({
    job: initialJob,
    resumeId,
    onBack,
    onAnalyzeMatch,
}) => {
    const [job, setJob] = useState<Job>(initialJob);
    const [loadingDetails, setLoadingDetails] = useState(false);
    const [matching, setMatching] = useState(false);
    const [showRawJD, setShowRawJD] = useState(false);
    const [copiedEmail, setCopiedEmail] = useState<string | null>(null);

    // Fetch full structured job details if not already present
    useEffect(() => {
        let isMounted = true;
        const fetchStructuredDetails = async () => {
            if (initialJob.structured) return;
            try {
                setLoadingDetails(true);
                const response = await apiRequest<ApiResponse<Job>>(`/api/jobs/${initialJob.id}`);
                if (isMounted && response?.success && response.data) {
                    setJob(response.data);
                }
            } catch (err) {
                console.warn('Could not load structured job details:', err);
            } finally {
                if (isMounted) setLoadingDetails(false);
            }
        };

        fetchStructuredDetails();
        return () => {
            isMounted = false;
        };
    }, [initialJob.id, initialJob.structured]);

    const handleMatch = async () => {
        try {
            setMatching(true);
            await onAnalyzeMatch(job);
        } finally {
            setMatching(false);
        }
    };

    const handleCopyEmail = (email: string) => {
        navigator.clipboard.writeText(email);
        setCopiedEmail(email);
        setTimeout(() => setCopiedEmail(null), 2000);
    };

    const structured = job.structured;

    // Resolve skills from structured JSON if not in top-level entity
    const requiredSkills =
        (job.requiredSkills && job.requiredSkills.length > 0)
            ? job.requiredSkills
            : structured?.requiredSkills && structured.requiredSkills.length > 0
            ? structured.requiredSkills
            : [];

    const preferredSkills =
        (job.preferredSkills && job.preferredSkills.length > 0)
            ? job.preferredSkills
            : structured?.preferredSkills && structured.preferredSkills.length > 0
            ? structured.preferredSkills
            : [];

    const hasSkillsInHero = requiredSkills.length > 0 || preferredSkills.length > 0;

    return (
        <div className="mx-auto max-w-4xl space-y-8 pb-12">
            {/* Navigation Header */}
            <div className="flex items-center justify-between">
                <button
                    onClick={onBack}
                    className="flex items-center gap-2 text-sm font-medium text-indigo-400 transition hover:text-indigo-300"
                >
                    <ArrowLeft size={16} />
                    Back to Jobs
                </button>

                <div className="flex items-center gap-3">
                    {loadingDetails && (
                        <span className="flex items-center gap-1.5 text-xs text-indigo-400">
                            <Loader2 size={13} className="animate-spin" />
                            Optimizing Description...
                        </span>
                    )}
                    <div className="flex items-center gap-2 text-xs text-slate-500">
                        <Clock size={14} />
                        Posted {new Date(job.createdAt).toLocaleDateString()}
                    </div>
                </div>
            </div>

            {/* Main Job Hero Banner */}
            <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 shadow-xl">
                <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
                    <div className="flex items-start gap-4">
                        <div className="rounded-2xl bg-indigo-500/10 p-4 border border-indigo-500/20 shrink-0">
                            <Briefcase size={32} className="text-indigo-400" />
                        </div>

                        <div>
                            <h1 className="text-2xl md:text-3xl font-extrabold text-white">
                                {job.title}
                            </h1>

                            <div className="mt-2 flex flex-wrap items-center gap-3 text-sm text-slate-400">
                                <span className="flex items-center gap-1.5 font-medium text-indigo-400">
                                    <Building2 size={16} />
                                    {job.company}
                                </span>

                                {job.location && (
                                    <span className="flex items-center gap-1.5">
                                        <MapPin size={16} className="text-slate-500" />
                                        {job.location}
                                    </span>
                                )}

                                {job.employmentType && (
                                    <span className="rounded-lg bg-slate-800 px-2.5 py-1 text-xs font-medium text-slate-300 border border-slate-700">
                                        {job.employmentType}
                                    </span>
                                )}
                            </div>
                        </div>
                    </div>

                    {/* Single Primary Action Button in Hero */}
                    <div className="shrink-0">
                        <button
                            type="button"
                            onClick={handleMatch}
                            disabled={!resumeId || matching}
                            className="flex w-full md:w-auto items-center justify-center gap-2 rounded-xl bg-indigo-600 px-6 py-3.5 text-sm font-semibold text-white shadow-lg shadow-indigo-500/20 transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                            {matching ? (
                                <>
                                    <Loader2 size={18} className="animate-spin" />
                                    Analyzing Match...
                                </>
                            ) : (
                                <>
                                    <Target size={18} />
                                    Analyze Match
                                </>
                            )}
                        </button>
                    </div>
                </div>

                {/* Hero Skills Badges (Rendered only if meaningful skills exist) */}
                {hasSkillsInHero && (
                    <div className="mt-8 border-t border-slate-800/80 pt-6 grid gap-6 md:grid-cols-2">
                        {requiredSkills.length > 0 && (
                            <div>
                                <p className="mb-3 text-xs font-bold uppercase tracking-wider text-emerald-400">
                                    Required Technical Skills
                                </p>
                                <div className="flex flex-wrap gap-2">
                                    {requiredSkills.map((skill) => (
                                        <span
                                            key={skill}
                                            className="rounded-lg border border-emerald-500/20 bg-emerald-500/10 px-3 py-1.5 text-xs font-medium text-emerald-300"
                                        >
                                            {skill}
                                        </span>
                                    ))}
                                </div>
                            </div>
                        )}

                        {preferredSkills.length > 0 && (
                            <div>
                                <p className="mb-3 text-xs font-bold uppercase tracking-wider text-purple-300">
                                    Preferred / Nice-to-Have Skills
                                </p>
                                <div className="flex flex-wrap gap-2">
                                    {preferredSkills.map((skill) => (
                                        <span
                                            key={skill}
                                            className="rounded-lg border border-purple-500/20 bg-purple-500/10 px-3 py-1.5 text-xs font-medium text-purple-200"
                                        >
                                            {skill}
                                        </span>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* ============================================================
                Structured Content Sections (Rendered ONLY if content exists)
                ============================================================ */}

            {loadingDetails && !structured ? (
                /* AI Structuring Loading Skeleton (Prevents flashing unformatted text) */
                <div className="rounded-3xl border border-indigo-500/20 bg-slate-900/90 p-8 md:p-12 shadow-xl flex flex-col items-center justify-center text-center space-y-5 backdrop-blur-sm">
                    <div className="relative flex items-center justify-center">
                        <div className="absolute h-16 w-16 rounded-full bg-indigo-500/20 animate-ping" />
                        <div className="rounded-2xl bg-indigo-600/20 p-4 border border-indigo-500/30">
                            <Sparkles size={32} className="text-indigo-400 animate-pulse" />
                        </div>
                    </div>

                    <div className="space-y-1.5">
                        <h3 className="text-lg font-bold text-white">
                            Structuring Job Description with AI
                        </h3>
                        <p className="text-xs text-slate-400 max-w-md">
                            Extracting responsibilities, culture & mindset, key prerequisites, and contact details...
                        </p>
                    </div>

                    <div className="flex items-center gap-2 text-xs text-indigo-400 pt-1 font-medium">
                        <Loader2 size={14} className="animate-spin" />
                        <span>Optimizing sections...</span>
                    </div>

                    {/* Animated Skeleton bars */}
                    <div className="w-full max-w-md space-y-2.5 pt-4">
                        <div className="h-3.5 bg-slate-800 rounded-full animate-pulse w-3/4 mx-auto" />
                        <div className="h-3 bg-slate-800/70 rounded-full animate-pulse w-5/6 mx-auto" />
                        <div className="h-3 bg-slate-800/40 rounded-full animate-pulse w-2/3 mx-auto" />
                    </div>
                </div>
            ) : structured ? (
                <div className="space-y-6">
                    {/* 1. About Company & Role */}
                    {(structured.aboutCompany || structured.teamAndRole) && (
                        <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 space-y-4 shadow-sm">
                            <div className="flex items-center gap-2.5 text-indigo-400">
                                <Building2 size={20} />
                                <h3 className="text-lg font-bold text-white">About the Company & Role</h3>
                            </div>
                            {structured.aboutCompany && (
                                <p className="text-sm leading-relaxed text-slate-300">
                                    {structured.aboutCompany}
                                </p>
                            )}
                            {structured.teamAndRole && (
                                <p className="text-sm leading-relaxed text-slate-400 border-l-2 border-indigo-500/40 pl-3">
                                    {structured.teamAndRole}
                                </p>
                            )}
                        </div>
                    )}

                    {/* 2. Why Join Us & Career Impact */}
                    {structured.whyJoinUs && structured.whyJoinUs.length > 0 && (
                        <div className="rounded-3xl border border-indigo-500/20 bg-gradient-to-br from-indigo-950/30 to-slate-900 p-6 md:p-8 shadow-sm">
                            <div className="flex items-center gap-2.5 text-indigo-400 mb-4">
                                <Sparkles size={20} />
                                <h3 className="text-lg font-bold text-white">Why Join Us & Impact</h3>
                            </div>
                            <ul className="grid gap-3 md:grid-cols-2">
                                {structured.whyJoinUs.map((item, idx) => (
                                    <li key={idx} className="flex items-start gap-2.5 text-sm text-slate-300">
                                        <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-indigo-400 shrink-0" />
                                        <span>{item}</span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* 3. Culture & Mindset */}
                    {structured.cultureAndMindset && structured.cultureAndMindset.length > 0 && (
                        <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 shadow-sm">
                            <div className="flex items-center gap-2.5 text-amber-400 mb-4">
                                <Sparkles size={20} />
                                <h3 className="text-lg font-bold text-white">Culture & Mindset</h3>
                            </div>
                            <div className="flex flex-wrap gap-2.5">
                                {structured.cultureAndMindset.map((trait, idx) => (
                                    <span
                                        key={idx}
                                        className="rounded-xl border border-amber-500/20 bg-amber-500/10 px-4 py-2 text-xs font-medium text-amber-200"
                                    >
                                        ✨ {trait}
                                    </span>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* 4. Core Responsibilities */}
                    {structured.responsibilities && structured.responsibilities.length > 0 && (
                        <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 shadow-sm">
                            <div className="flex items-center gap-2.5 text-indigo-400 mb-4">
                                <CheckCircle2 size={20} />
                                <h3 className="text-lg font-bold text-white">Key Responsibilities</h3>
                            </div>
                            <ul className="space-y-3">
                                {structured.responsibilities.map((resp, idx) => (
                                    <li key={idx} className="flex items-start gap-3 text-sm leading-relaxed text-slate-300">
                                        <CheckCircle2 size={16} className="mt-1 text-indigo-400 shrink-0" />
                                        <span>{resp}</span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* 5. Qualifications & Experience */}
                    {structured.qualifications && structured.qualifications.length > 0 && (
                        <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 shadow-sm">
                            <div className="flex items-center gap-2.5 text-indigo-400 mb-4">
                                <GraduationCap size={20} />
                                <h3 className="text-lg font-bold text-white">Qualifications & Requirements</h3>
                            </div>
                            <ul className="space-y-3">
                                {structured.qualifications.map((qual, idx) => (
                                    <li key={idx} className="flex items-start gap-3 text-sm leading-relaxed text-slate-300">
                                        <div className="mt-2 h-1.5 w-1.5 rounded-full bg-indigo-400 shrink-0" />
                                        <span>{qual}</span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* 6. Benefits & Perks */}
                    {structured.benefits && structured.benefits.length > 0 && (
                        <div className="rounded-3xl border border-emerald-500/20 bg-slate-900 p-6 md:p-8 shadow-sm">
                            <div className="flex items-center gap-2.5 text-emerald-400 mb-4">
                                <Gift size={20} />
                                <h3 className="text-lg font-bold text-white">Benefits & Perks</h3>
                            </div>
                            <div className="grid gap-3 md:grid-cols-2">
                                {structured.benefits.map((benefit, idx) => (
                                    <div
                                        key={idx}
                                        className="flex items-start gap-3 rounded-2xl border border-slate-800 bg-slate-950/60 p-3.5 text-sm text-slate-300"
                                    >
                                        <span className="text-emerald-400">🎁</span>
                                        <span>{benefit}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* 7. How to Apply Callout */}
                    {structured.howToApply &&
                        (structured.howToApply.email ||
                            structured.howToApply.url ||
                            structured.howToApply.instructions) && (
                            <div className="rounded-3xl border border-indigo-500/30 bg-gradient-to-r from-indigo-950/50 via-slate-900 to-slate-900 p-6 md:p-8 shadow-lg">
                                <div className="flex items-center gap-2.5 text-indigo-400 mb-3">
                                    <Mail size={20} />
                                    <h3 className="text-lg font-bold text-white">How to Apply</h3>
                                </div>

                                {structured.howToApply.instructions && (
                                    <p className="text-sm text-slate-300 mb-4">
                                        {structured.howToApply.instructions}
                                    </p>
                                )}

                                <div className="flex flex-wrap items-center gap-3">
                                    {structured.howToApply.email &&
                                        structured.howToApply.email
                                            .split(/[,;\s]+/)
                                            .filter((e) => e.includes('@'))
                                            .map((email, eIdx) => (
                                                <button
                                                    key={eIdx}
                                                    type="button"
                                                    onClick={() => handleCopyEmail(email)}
                                                    className="flex items-center gap-2 rounded-xl border border-indigo-500/30 bg-indigo-500/10 px-4 py-2.5 text-sm font-medium text-indigo-300 hover:bg-indigo-500/20 transition"
                                                >
                                                    {copiedEmail === email ? (
                                                        <Check size={16} className="text-emerald-400" />
                                                    ) : (
                                                        <Copy size={16} />
                                                    )}
                                                    <span>{email}</span>
                                                    {copiedEmail === email && (
                                                        <span className="text-xs text-emerald-400 ml-1">Copied!</span>
                                                    )}
                                                </button>
                                            ))}

                                    {structured.howToApply.url && (
                                        <a
                                            href={structured.howToApply.url}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-indigo-500 transition shadow-md"
                                        >
                                            <span>Apply on Company Portal</span>
                                            <ExternalLink size={15} />
                                        </a>
                                    )}
                                </div>
                            </div>
                        )}

                    {/* 8. Additional Custom Sections */}
                    {structured.additionalSections && structured.additionalSections.length > 0 && (
                        <div className="space-y-4">
                            {structured.additionalSections.map((sec, idx) => (
                                <div key={idx} className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8 shadow-sm">
                                    <div className="flex items-center gap-2 text-indigo-400 mb-3">
                                        <Layers size={18} />
                                        <h4 className="text-base font-bold text-white">{sec.title}</h4>
                                    </div>
                                    {sec.content && (
                                        <p className="text-sm leading-relaxed text-slate-300 mb-3">{sec.content}</p>
                                    )}
                                    {sec.items && sec.items.length > 0 && (
                                        <ul className="space-y-2">
                                            {sec.items.map((item, itemIdx) => (
                                                <li key={itemIdx} className="flex items-start gap-2.5 text-sm text-slate-300">
                                                    <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-indigo-400 shrink-0" />
                                                    <span>{item}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}

                    {/* 9. Collapsible Original Job Description Toggle */}
                    <div className="rounded-2xl border border-slate-800/80 bg-slate-900/50 p-4">
                        <button
                            type="button"
                            onClick={() => setShowRawJD(!showRawJD)}
                            className="flex w-full items-center justify-between text-xs font-semibold text-slate-400 hover:text-slate-200 transition"
                        >
                            <span className="flex items-center gap-2">
                                <FileText size={15} />
                                View Original Job Posting (Unformatted Source of Truth)
                            </span>
                            {showRawJD ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                        </button>

                        {showRawJD && (
                            <div className="mt-4 border-t border-slate-800 pt-4 whitespace-pre-wrap font-sans text-xs leading-relaxed text-slate-400 bg-slate-950/60 p-4 rounded-xl">
                                {job.description}
                            </div>
                        )}
                    </div>
                </div>
            ) : (
                /* Fallback: If structured data is not yet generated and not loading, render raw description cleanly */
                <div className="rounded-3xl border border-slate-800 bg-slate-900 p-6 md:p-8">
                    <h3 className="text-lg font-bold text-white mb-6">
                        Complete Job Description
                    </h3>

                    <div className="whitespace-pre-wrap font-sans text-sm leading-relaxed text-slate-300">
                        {job.description}
                    </div>
                </div>
            )}
        </div>
    );
};
