import React from 'react';
import {
    Award,
    Code,
    GraduationCap,
    Mail,
    CheckCircle2,
    AlertTriangle,
    Search,
    AlertCircle,
    Lightbulb,
    ArrowLeft,
} from 'lucide-react';

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
    onBack: () => void;
}

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
    onBack,
}) => {
    const categories = breakdown
        ? Object.entries(breakdown)
        : [];

    return (
        <div className="w-full max-w-5xl space-y-6">

            {/* Back */}
            <button
                type="button"
                onClick={onBack}
                className="flex items-center gap-2 text-sm font-medium text-indigo-400 transition hover:text-indigo-300"
            >
                <ArrowLeft size={16} />
                Back to Dashboard
            </button>

            {/* Candidate */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                        <p className="text-xs uppercase tracking-wider text-slate-500">
                            Candidate
                        </p>

                        <h2 className="mt-1 text-xl font-bold text-white">
                            {name || 'Candidate'}
                        </h2>
                    </div>

                    <div className="flex flex-wrap gap-4 text-xs text-slate-400">
                        {email && (
                            <span className="flex items-center gap-2">
                                <Mail size={14} />
                                {email}
                            </span>
                        )}

                        {phone && (
                            <span>
                                {phone}
                            </span>
                        )}
                    </div>
                </div>
            </div>

            {/* Score + Breakdown */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">

                <div className="flex flex-col items-center justify-center rounded-2xl border border-slate-800 bg-slate-900 p-8 text-center">

                    <div
                        className={`flex h-40 w-40 flex-col items-center justify-center rounded-full border-4 ${getScoreClass(score)}`}
                    >
                        <span className="text-5xl font-black">
                            {score}
                        </span>

                        <span className="text-xs font-semibold uppercase tracking-wider opacity-70">
                            / 100
                        </span>
                    </div>

                    <h3 className="mt-5 flex items-center gap-2 font-semibold text-white">
                        {score >= 80 ? (
                            <CheckCircle2
                                size={18}
                                className="text-emerald-400"
                            />
                        ) : (
                            <AlertTriangle
                                size={18}
                                className="text-amber-400"
                            />
                        )}

                        {getScoreLabel(score)}
                    </h3>

                    {analyzedAt && (
                        <p className="mt-2 text-xs text-slate-500">
                            Analyzed{' '}
                            {new Date(analyzedAt).toLocaleString()}
                        </p>
                    )}
                </div>

                <div className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900 p-6 lg:col-span-2">

                    <div>
                        <h3 className="text-sm font-semibold text-white">
                            ATS Category Breakdown
                        </h3>

                        <p className="mt-1 text-xs text-slate-500">
                            Scores returned by the AI analysis pipeline.
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

                                <span className="text-slate-500">
                                    {value}
                                </span>
                            </div>

                            <div className="h-2 overflow-hidden rounded-full bg-slate-800">
                                <div
                                    className="h-full rounded-full bg-indigo-500 transition-all"
                                    style={{
                                        width: `${Math.min(
                                            Math.max(value, 0),
                                            100
                                        )}%`,
                                    }}
                                />
                            </div>

                        </div>
                    ))}
                </div>
            </div>

            {/* Information */}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">

                <InfoPanel
                    title="Skills Identified"
                    icon={<Code size={17} />}
                    items={skills}
                    empty="No skills identified."
                    pillClass="bg-indigo-500/10 text-indigo-300 border-indigo-500/20"
                />

                <InfoPanel
                    title="Keywords Identified"
                    icon={<Search size={17} />}
                    items={keywords}
                    empty="No keywords identified."
                    pillClass="bg-slate-800 text-slate-300 border-slate-700"
                />

                <InfoPanel
                    title="Missing Skills"
                    icon={<GraduationCap size={17} />}
                    items={missingSkills}
                    empty="No missing skills reported."
                    pillClass="bg-amber-500/10 text-amber-300 border-amber-500/20"
                />

                <InfoPanel
                    title="Missing Keywords"
                    icon={<Search size={17} />}
                    items={missingKeywords}
                    empty="No missing keywords reported."
                    pillClass="bg-rose-500/10 text-rose-300 border-rose-500/20"
                />

            </div>

            <ListPanel
                title="Issues"
                icon={<AlertCircle size={17} />}
                items={issues}
                empty="No issues reported."
            />

            <ListPanel
                title="Recommendations"
                icon={<Lightbulb size={17} />}
                items={recommendations}
                empty="No recommendations reported."
            />

        </div>
    );
};

interface InfoPanelProps {
    title: string;
    icon: React.ReactNode;
    items: string[];
    empty: string;
    pillClass: string;
}

const InfoPanel: React.FC<InfoPanelProps> = ({
    title,
    icon,
    items,
    empty,
    pillClass,
}) => (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">

        <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
            {icon}
            {title}
        </h3>

        {items.length === 0 ? (
            <p className="mt-4 text-xs text-slate-500">
                {empty}
            </p>
        ) : (
            <div className="mt-4 flex flex-wrap gap-2">
                {items.map((item, index) => (
                    <span
                        key={`${item}-${index}`}
                        className={`rounded-lg border px-3 py-1.5 text-xs font-medium ${pillClass}`}
                    >
                        {item}
                    </span>
                ))}
            </div>
        )}

    </div>
);

interface ListPanelProps {
    title: string;
    icon: React.ReactNode;
    items: string[];
    empty: string;
}

const ListPanel: React.FC<ListPanelProps> = ({
    title,
    icon,
    items,
    empty,
}) => (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">

        <h3 className="flex items-center gap-2 text-sm font-semibold text-white">
            {icon}
            {title}
        </h3>

        {items.length === 0 ? (
            <p className="mt-4 text-xs text-slate-500">
                {empty}
            </p>
        ) : (
            <ul className="mt-4 space-y-3">
                {items.map((item, index) => (
                    <li
                        key={`${item}-${index}`}
                        className="flex gap-3 text-sm text-slate-300"
                    >
                        <span className="mt-1 shrink-0 text-indigo-400">
                            •
                        </span>

                        <span>{item}</span>
                    </li>
                ))}
            </ul>
        )}

    </div>
);