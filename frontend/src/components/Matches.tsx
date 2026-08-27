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
            ) : loading ? (
                <div className="flex items-center justify-center rounded-2xl border border-slate-800 bg-slate-900 p-12">
                    <Loader2
                        size={30}
                        className="animate-spin text-indigo-400"
                    />
                </div>
            ) : error ? (
                <div className="flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                    <AlertCircle size={18} className="shrink-0" />
                    <span>{error}</span>
                </div>
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
            ) : (
                <div className="space-y-5">
                    {matches.map((match) => (
                        <div
                            key={match.id}
                            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
                        >
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

                            {match.reasons &&
                                match.reasons.length > 0 && (
                                    <div className="mt-5">
                                        <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                            Why this match
                                        </p>

                                        <ul className="space-y-2 text-sm text-slate-400">
                                            {match.reasons.map(
                                                (reason, index) => (
                                                    <li key={index}>
                                                        • {reason}
                                                    </li>
                                                )
                                            )}
                                        </ul>
                                    </div>
                                )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};