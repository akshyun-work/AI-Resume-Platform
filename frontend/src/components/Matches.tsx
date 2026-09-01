import React, { useEffect, useState } from 'react';
import {
    Target,
    Loader2,
    AlertCircle,
    RefreshCw,
} from 'lucide-react';

import { apiRequest } from '../services/api';

interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string | null;
}

interface WhyThisMatch {
    matchSummary?: string | null;
    whyYouMatch?: string[] | null;

    missingRequiredSkills?: string[] | null;
    missingPreferredSkills?: string[] | null;
    missingJobSpecificSkills?: string[] | null;

    score?: number | null;
    scoreExplanation?: string | null;
    scoreFactors?: string[] | null;

    improvementActions?: string[] | null;
}

interface Match {
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

interface MatchesProps {
    resumeId?: string;
    onBack: () => void;
}

/**
 * Converts the structured Gemini response currently stored
 * inside reasons[0] into the frontend Why This Match model.
 *
 * Current backend structure:
 *
 * reasons: [
 *   "{ ...structured gemini_analysis JSON... }"
 * ]
 */
const parseWhyThisMatch = (
    match: Match
): WhyThisMatch | null => {
    if (!match.reasons || match.reasons.length === 0) {
        return null;
    }

    for (const reason of match.reasons) {
        if (!reason || typeof reason !== 'string') {
            continue;
        }

        try {
            const parsed = JSON.parse(reason);

            if (
                parsed &&
                typeof parsed === 'object' &&
                (
                    'match_summary' in parsed ||
                    'why_you_match' in parsed ||
                    'what_is_missing' in parsed ||
                    'score_explanation' in parsed ||
                    'improvement_actions' in parsed
                )
            ) {
                return {
                    matchSummary:
                        typeof parsed.match_summary === 'string'
                            ? parsed.match_summary
                            : null,

                    whyYouMatch:
                        Array.isArray(parsed.why_you_match)
                            ? parsed.why_you_match
                            : null,

                    missingRequiredSkills:
                        Array.isArray(
                            parsed.what_is_missing?.required
                        )
                            ? parsed.what_is_missing.required
                            : null,

                    missingPreferredSkills:
                        Array.isArray(
                            parsed.what_is_missing?.preferred
                        )
                            ? parsed.what_is_missing.preferred
                            : null,

                    missingJobSpecificSkills:
                        Array.isArray(
                            parsed.what_is_missing?.job_specific
                        )
                            ? parsed.what_is_missing.job_specific
                            : null,

                    score:
                        typeof parsed.score_explanation?.score === 'number'
                            ? parsed.score_explanation.score
                            : null,

                    scoreExplanation:
                        typeof parsed.score_explanation?.explanation === 'string'
                            ? parsed.score_explanation.explanation
                            : null,

                    scoreFactors:
                        Array.isArray(
                            parsed.score_explanation?.factors
                        )
                            ? parsed.score_explanation.factors
                            : null,

                    improvementActions:
                        Array.isArray(parsed.improvement_actions)
                            ? parsed.improvement_actions
                            : null,
                };
            }
        } catch {
            // Old plain-text Reasons value.
            // Continue looking for a structured reason.
        }
    }

    return null;
};

export const Matches: React.FC<MatchesProps> = ({
    resumeId,
    onBack,
}) => {
    const [matches, setMatches] = useState<Match[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const loadMatches = async () => {
        if (!resumeId) {
            setMatches([]);
            setLoading(false);
            return;
        }

        try {
            setLoading(true);
            setError(null);

            const response =
                await apiRequest<ApiResponse<Match[]>>(
                    `/api/matches/resume/${resumeId}`
                );

            if (!response.success || !response.data) {
                throw new Error(
                    response.message || 'Unable to load matches.'
                );
            }

            setMatches(response.data);
        } catch (err) {
            console.error(err);

            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to load matches.'
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadMatches();
    }, [resumeId]);

    const scoreClass = (score: number) => {
        if (score >= 80) {
            return 'text-emerald-400';
        }

        if (score >= 60) {
            return 'text-amber-400';
        }

        return 'text-rose-400';
    };

    return (
        <div className="space-y-8">

            {/* PAGE HEADER */}
            <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
                <div>
                    <button
                        onClick={onBack}
                        className="mb-3 text-sm text-indigo-400 hover:text-indigo-300"
                    >
                        ← Back to Dashboard
                    </button>

                    <h2 className="text-3xl font-bold text-white">
                        Job Matches
                    </h2>

                    <p className="mt-2 text-slate-400">
                        View job compatibility results for your latest resume.
                    </p>
                </div>

                <button
                    onClick={loadMatches}
                    disabled={loading}
                    className="flex items-center justify-center gap-2 rounded-xl border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-50"
                >
                    <RefreshCw size={16} />
                    Refresh
                </button>
            </div>

            {/* NO RESUME */}
            {!resumeId ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-12 text-center">
                    <Target
                        size={42}
                        className="mx-auto mb-4 text-slate-600"
                    />

                    <h3 className="text-lg font-bold text-white">
                        Upload a resume first
                    </h3>

                    <p className="mt-2 text-sm text-slate-400">
                        Job matches require an uploaded resume.
                    </p>
                </div>

                /* LOADING */
            ) : loading ? (
                <div className="flex items-center justify-center rounded-2xl border border-slate-800 bg-slate-900 p-12">
                    <Loader2
                        size={30}
                        className="animate-spin text-indigo-400"
                    />
                </div>

                /* ERROR */
            ) : error ? (
                <div className="flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                    <AlertCircle size={18} className="shrink-0" />
                    <span>{error}</span>
                </div>

                /* NO MATCHES */
            ) : matches.length === 0 ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-12 text-center">
                    <Target
                        size={42}
                        className="mx-auto mb-4 text-slate-600"
                    />

                    <h3 className="text-lg font-bold text-white">
                        No matches yet
                    </h3>

                    <p className="mt-2 text-sm text-slate-400">
                        No matching jobs have been generated for this resume yet.
                    </p>
                </div>

                /* MATCHES */
            ) : (
                <div className="space-y-5">

                    {matches.map((match) => {
                        const whyThisMatch =
                            parseWhyThisMatch(match);

                        return (
                            <div
                                key={match.id}
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
                            >

                                {/* JOB HEADER */}
                                <div className="flex flex-col gap-5 md:flex-row md:items-start md:justify-between">
                                    <div>
                                        <h3 className="text-xl font-bold text-white">
                                            {match.jobTitle}
                                        </h3>

                                        <p className="mt-1 text-sm text-indigo-400">
                                            {match.company}
                                        </p>
                                    </div>

                                    <div className="text-left md:text-right">
                                        <p className="text-xs uppercase tracking-wider text-slate-500">
                                            Match Score
                                        </p>

                                        <p
                                            className={`mt-1 text-3xl font-bold ${scoreClass(
                                                match.matchScore
                                            )}`}
                                        >
                                            {match.matchScore}
                                            <span className="text-sm text-slate-500">
                                                /100
                                            </span>
                                        </p>
                                    </div>
                                </div>

                                {/* SCORE BAR */}
                                <div className="mt-6 h-2 overflow-hidden rounded-full bg-slate-800">
                                    <div
                                        className="h-full rounded-full bg-indigo-500"
                                        style={{
                                            width: `${Math.min(
                                                100,
                                                Math.max(
                                                    0,
                                                    match.matchScore
                                                )
                                            )}%`,
                                        }}
                                    />
                                </div>

                                {/* MATCHING SKILLS */}
                                {match.matchingSkills &&
                                    match.matchingSkills.length > 0 && (
                                        <div className="mt-6">
                                            <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                                Matching Skills
                                            </p>

                                            <div className="flex flex-wrap gap-2">
                                                {match.matchingSkills.map(
                                                    (skill) => (
                                                        <span
                                                            key={skill}
                                                            className="rounded-lg bg-emerald-500/10 px-2 py-1 text-xs text-emerald-300"
                                                        >
                                                            {skill}
                                                        </span>
                                                    )
                                                )}
                                            </div>
                                        </div>
                                    )}

                                {/* MISSING SKILLS */}
                                {match.missingSkills &&
                                    match.missingSkills.length > 0 && (
                                        <div className="mt-5">
                                            <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                                Missing Skills
                                            </p>

                                            <div className="flex flex-wrap gap-2">
                                                {match.missingSkills.map(
                                                    (skill) => (
                                                        <span
                                                            key={skill}
                                                            className="rounded-lg bg-rose-500/10 px-2 py-1 text-xs text-rose-300"
                                                        >
                                                            {skill}
                                                        </span>
                                                    )
                                                )}
                                            </div>
                                        </div>
                                    )}

                                {/* =========================================
                                    WHY THIS MATCH
                                ========================================= */}

                                {whyThisMatch && (
                                    <div className="mt-7 border-t border-slate-800 pt-7">

                                        {/* MAIN TITLE */}
                                        <p className="mb-6 text-xs uppercase tracking-wider text-slate-500">
                                            Why This Match
                                        </p>

                                        {/* MATCH SUMMARY */}
                                        {whyThisMatch.matchSummary && (
                                            <div className="mb-7">
                                                <h4 className="mb-3 text-sm font-semibold text-white">
                                                    Match Summary
                                                </h4>

                                                <p className="max-w-4xl text-sm leading-6 text-slate-400">
                                                    {whyThisMatch.matchSummary}
                                                </p>
                                            </div>
                                        )}

                                        {/* WHY YOU MATCH */}
                                        {whyThisMatch.whyYouMatch &&
                                            whyThisMatch.whyYouMatch.length > 0 && (
                                                <div className="mb-7">
                                                    <h4 className="mb-3 text-sm font-semibold text-white">
                                                        Why You Match
                                                    </h4>

                                                    <ul className="space-y-2 text-sm text-slate-400">
                                                        {whyThisMatch.whyYouMatch.map(
                                                            (skill) => (
                                                                <li
                                                                    key={skill}
                                                                    className="flex items-center gap-2"
                                                                >
                                                                    <span className="text-emerald-400">
                                                                        ✓
                                                                    </span>

                                                                    <span>
                                                                        {skill}
                                                                    </span>
                                                                </li>
                                                            )
                                                        )}
                                                    </ul>
                                                </div>
                                            )}

                                        {/* WHAT IS MISSING */}
                                        {(
                                            whyThisMatch.missingRequiredSkills?.length ||
                                            whyThisMatch.missingPreferredSkills?.length ||
                                            whyThisMatch.missingJobSpecificSkills?.length
                                        ) ? (
                                            <div className="mb-7">
                                                <h4 className="mb-4 text-sm font-semibold text-white">
                                                    What Is Missing
                                                </h4>

                                                {/* REQUIRED */}
                                                {whyThisMatch.missingRequiredSkills &&
                                                    whyThisMatch.missingRequiredSkills.length > 0 && (
                                                        <div className="mb-5">
                                                            <p className="mb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                                                                Required
                                                            </p>

                                                            <ul className="space-y-2 text-sm text-slate-400">
                                                                {whyThisMatch.missingRequiredSkills.map(
                                                                    (skill) => (
                                                                        <li
                                                                            key={skill}
                                                                            className="flex items-center gap-2"
                                                                        >
                                                                            <span className="text-rose-400">
                                                                                •
                                                                            </span>

                                                                            <span>
                                                                                {skill}
                                                                            </span>
                                                                        </li>
                                                                    )
                                                                )}
                                                            </ul>
                                                        </div>
                                                    )}

                                                {/* PREFERRED */}
                                                {whyThisMatch.missingPreferredSkills &&
                                                    whyThisMatch.missingPreferredSkills.length > 0 && (
                                                        <div className="mb-5">
                                                            <p className="mb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                                                                Preferred
                                                            </p>

                                                            <ul className="space-y-2 text-sm text-slate-400">
                                                                {whyThisMatch.missingPreferredSkills.map(
                                                                    (skill) => (
                                                                        <li
                                                                            key={skill}
                                                                            className="flex items-center gap-2"
                                                                        >
                                                                            <span className="text-amber-400">
                                                                                •
                                                                            </span>

                                                                            <span>
                                                                                {skill}
                                                                            </span>
                                                                        </li>
                                                                    )
                                                                )}
                                                            </ul>
                                                        </div>
                                                    )}

                                                {/* JOB SPECIFIC */}
                                                {whyThisMatch.missingJobSpecificSkills &&
                                                    whyThisMatch.missingJobSpecificSkills.length > 0 && (
                                                        <div>
                                                            <p className="mb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                                                                Job-Specific
                                                            </p>

                                                            <ul className="space-y-2 text-sm text-slate-400">
                                                                {whyThisMatch.missingJobSpecificSkills.map(
                                                                    (skill) => (
                                                                        <li
                                                                            key={skill}
                                                                            className="flex items-center gap-2"
                                                                        >
                                                                            <span className="text-slate-400">
                                                                                •
                                                                            </span>

                                                                            <span>
                                                                                {skill}
                                                                            </span>
                                                                        </li>
                                                                    )
                                                                )}
                                                            </ul>
                                                        </div>
                                                    )}
                                            </div>
                                        ) : null}

                                        {/* SCORE EXPLANATION */}
                                        {(
                                            whyThisMatch.score !== null &&
                                            whyThisMatch.score !== undefined
                                        ) ||
                                            whyThisMatch.scoreExplanation ||
                                            whyThisMatch.scoreFactors?.length ? (
                                            <div className="mb-7">
                                                <h4 className="mb-3 text-sm font-semibold text-white">
                                                    Score Explanation
                                                </h4>

                                                <p className="mb-4 text-2xl font-bold text-white">
                                                    {whyThisMatch.score ??
                                                        match.matchScore}
                                                    <span className="text-sm font-normal text-slate-500">
                                                        /100
                                                    </span>
                                                </p>

                                                {whyThisMatch.scoreExplanation && (
                                                    <p className="mb-4 max-w-4xl text-sm leading-6 text-slate-400">
                                                        {
                                                            whyThisMatch.scoreExplanation
                                                        }
                                                    </p>
                                                )}

                                                {whyThisMatch.scoreFactors &&
                                                    whyThisMatch.scoreFactors.length > 0 && (
                                                        <ul className="space-y-2 text-sm text-slate-400">
                                                            {whyThisMatch.scoreFactors.map(
                                                                (factor) => (
                                                                    <li
                                                                        key={factor}
                                                                        className="flex items-start gap-2"
                                                                    >
                                                                        <span className="text-rose-400">
                                                                            •
                                                                        </span>

                                                                        <span>
                                                                            {factor}
                                                                        </span>
                                                                    </li>
                                                                )
                                                            )}
                                                        </ul>
                                                    )}
                                            </div>
                                        ) : null}

                                        {/* HOW TO IMPROVE THIS MATCH */}
                                        {whyThisMatch.improvementActions &&
                                            whyThisMatch.improvementActions.length > 0 && (
                                                <div>
                                                    <h4 className="mb-3 text-sm font-semibold text-white">
                                                        How to Improve This Match
                                                    </h4>

                                                    <ol className="space-y-3 text-sm leading-6 text-slate-400">
                                                        {whyThisMatch.improvementActions.map(
                                                            (action, index) => (
                                                                <li
                                                                    key={`${index}-${action}`}
                                                                    className="flex items-start gap-3"
                                                                >
                                                                    <span className="font-semibold text-indigo-400">
                                                                        {index + 1}.
                                                                    </span>

                                                                    <span>
                                                                        {action}
                                                                    </span>
                                                                </li>
                                                            )
                                                        )}
                                                    </ol>
                                                </div>
                                            )}

                                    </div>
                                )}

                            </div>
                        );
                    })}

                </div>
            )}
        </div>
    );
};