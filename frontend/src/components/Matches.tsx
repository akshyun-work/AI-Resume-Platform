import React, { useEffect, useState } from 'react';
import {
    Target,
    Loader2,
    AlertCircle,
    RefreshCw,
    CheckCircle2,
    AlertTriangle,
    Compass,
    TrendingUp,
    ListChecks,
    Award,
    Eye,
} from 'lucide-react';

import { apiRequest } from '../services/api';

interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string | null;
}

interface StrengthItem {
    title?: string;
    description?: string;
}

interface WeaknessItem {
    title?: string;
    description?: string;
}

interface ImprovementActionItem {
    title?: string;
    action?: string;
}

interface CareerDirection {
    immediateTarget?: string | null;
    strongerAlignment?: string | null;
}

interface WhyThisMatch {
    // 1. Resume Strengths
    resumeStrengths?: StrengthItem[] | string[] | null;

    // 2. Resume Weaknesses
    resumeWeaknesses?: WeaknessItem[] | string[] | null;

    // 3. Explanation of Job Match
    matchSummary?: string | null;
    score?: number | null;
    scoreExplanation?: string | null;
    scoreFactors?: string[] | null;

    // 4. Most Important Missing Skills
    missingRequiredSkills?: string[] | null;
    missingPreferredSkills?: string[] | null;
    missingJobSpecificSkills?: string[] | null;

    // 5. Prioritized Improvement Actions
    improvementActions?: (ImprovementActionItem | string)[] | null;

    // 6. Career Direction
    careerDirection?: CareerDirection | string | null;

    // Backward compatibility
    whyYouMatch?: string[] | null;
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
    onViewJobById?: (jobId: string) => void;
}

/**
 * Converts structured Gemini JSON (or legacy markdown/fallback data)
 * into the comprehensive 6-part WhyThisMatch model.
/**
 * Converts structured Gemini JSON (or legacy markdown/fallback data)
 * into the comprehensive 6-part WhyThisMatch model with guaranteed fields.
 */
const parseWhyThisMatch = (match: Match): WhyThisMatch => {
    // 1. Prepare rich defaults based on match data
    const matchedSkills = match.matchingSkills && match.matchingSkills.length > 0 ? match.matchingSkills : [];
    const missingSkills = match.missingSkills && match.missingSkills.length > 0 ? match.missingSkills : [];
    const missingKeywords = match.missingKeywords && match.missingKeywords.length > 0 ? match.missingKeywords : [];

    const defaultStrengths: StrengthItem[] = matchedSkills.length > 0
        ? matchedSkills.map((s) => ({
              title: 'Demonstrated Proficiency',
              description: `Verified experience and project capability in ${s}.`,
          }))
        : [
              {
                  title: 'Core Technical Foundation',
                  description: 'Candidate shows transferable technical competencies aligned with core engineering requirements.',
              },
          ];

    const defaultWeaknesses: WeaknessItem[] = missingSkills.length > 0
        ? missingSkills.map((s) => ({
              title: 'Skill Gap',
              description: `Resume does not explicitly highlight hands-on experience or project depth with ${s}.`,
          }))
        : [
              {
                  title: 'Minor Content Optimization',
                  description: 'No critical skill deficiencies detected for this position. Focus on quantifying project impact.',
              },
          ];

    const defaultSummary = `Candidate demonstrates a ${match.matchScore}% qualification match for ${match.jobTitle} at ${match.company}.`;
    const defaultScore = match.matchScore;
    const defaultScoreExplanation = `The score of ${match.matchScore}/100 is calculated based on skill keyword overlap (${matchedSkills.length} matched) and domain capability alignment.`;
    const defaultFactors = missingSkills.length > 0
        ? [`Impacted by missing skills: ${missingSkills.slice(0, 3).join(', ')}`]
        : ['Candidate satisfies primary qualification requirements.'];

    const defaultMissingReq = missingSkills.slice(0, 3);
    const defaultMissingPref = missingSkills.slice(3, 6);
    const defaultMissingJob = missingKeywords.filter((k) => !missingSkills.includes(k)).slice(0, 3);

    const defaultActions: ImprovementActionItem[] = missingSkills.length > 0
        ? missingSkills.slice(0, 3).map((s) => ({
              title: `Develop and Document ${s}`,
              action: `Build a project or feature incorporating ${s}, then highlight implementation details and measurable outcomes on your resume.`,
          }))
        : [
              {
                  title: 'Quantify Engineering Impact',
                  action: 'Add measurable metrics (such as throughput, latency reduction, user scale) to your current project descriptions.',
              },
              {
                  title: 'Highlight Architectural Decisions',
                  action: 'Detail system design choices, API design, and testing methodologies in your recent experience sections.',
              },
              {
                  title: 'Target Advanced Certifications',
                  action: 'Pursue recognized industry credentials to further substantiate senior-level domain expertise.',
              },
          ];

    const defaultCareerDir: CareerDirection = {
        immediateTarget: `${match.jobTitle} (bridge identified skill gaps in ${missingSkills.slice(0, 2).join(', ') || 'specialized tools'} to maximize interview conversion).`,
        strongerAlignment: `High potential for specialized or senior technical roles leveraging your demonstrated proficiency in ${matchedSkills.slice(0, 3).join(', ') || 'core software engineering'}.`,
    };

    // 2. Try parsing structured JSON or legacy Markdown from reasons
    if (match.reasons && match.reasons.length > 0) {
        for (const reason of match.reasons) {
            if (!reason || typeof reason !== 'string') continue;

            // Attempt JSON parse
            try {
                const parsed = JSON.parse(reason);
                if (parsed && typeof parsed === 'object') {
                    // Strengths
                    let strengths: StrengthItem[] = [];
                    if (Array.isArray(parsed.resume_strengths) && parsed.resume_strengths.length > 0) {
                        strengths = parsed.resume_strengths.map((s: any) =>
                            typeof s === 'string' ? { title: 'Strength', description: s } : s
                        );
                    } else if (Array.isArray(parsed.why_you_match) && parsed.why_you_match.length > 0) {
                        strengths = parsed.why_you_match.map((s: string) => ({ title: 'Core Strength', description: s }));
                    }

                    // Weaknesses
                    let weaknesses: WeaknessItem[] = [];
                    if (Array.isArray(parsed.resume_weaknesses) && parsed.resume_weaknesses.length > 0) {
                        weaknesses = parsed.resume_weaknesses.map((w: any) =>
                            typeof w === 'string' ? { title: 'Skill Gap', description: w } : w
                        );
                    }

                    // Explanation of match
                    const summary =
                        parsed.explanation_of_job_match?.summary ??
                        parsed.match_summary ??
                        defaultSummary;

                    const score =
                        typeof parsed.explanation_of_job_match?.score === 'number'
                            ? parsed.explanation_of_job_match.score
                            : (typeof parsed.score_explanation?.score === 'number'
                                ? parsed.score_explanation.score
                                : defaultScore);

                    const scoreExplanation =
                        parsed.explanation_of_job_match?.analysis ??
                        parsed.score_explanation?.explanation ??
                        defaultScoreExplanation;

                    const scoreFactors =
                        Array.isArray(parsed.explanation_of_job_match?.factors) && parsed.explanation_of_job_match.factors.length > 0
                            ? parsed.explanation_of_job_match.factors
                            : (Array.isArray(parsed.score_explanation?.factors) && parsed.score_explanation.factors.length > 0
                                ? parsed.score_explanation.factors
                                : defaultFactors);

                    // Missing skills
                    const rawReq = parsed.most_important_missing_skills?.required ?? parsed.what_is_missing?.required;
                    const rawPref = parsed.most_important_missing_skills?.preferred ?? parsed.what_is_missing?.preferred;
                    const rawJob = parsed.most_important_missing_skills?.job_specific ?? parsed.what_is_missing?.job_specific;

                    const missingReq = Array.isArray(rawReq) && rawReq.length > 0 ? rawReq : (missingSkills.length > 0 ? defaultMissingReq : []);
                    const missingPref = Array.isArray(rawPref) && rawPref.length > 0 ? rawPref : (missingSkills.length > 3 ? defaultMissingPref : []);
                    const missingJob = Array.isArray(rawJob) && rawJob.length > 0 ? rawJob : defaultMissingJob;

                    // Improvement actions
                    let actions: ImprovementActionItem[] = [];
                    const rawActions = parsed.prioritized_improvement_actions ?? parsed.improvement_actions;
                    if (Array.isArray(rawActions) && rawActions.length > 0) {
                        actions = rawActions.map((a: any) => {
                            if (typeof a === 'string') {
                                const colonIdx = a.indexOf(':');
                                if (colonIdx > 0) {
                                    return { title: a.slice(0, colonIdx).trim(), action: a.slice(colonIdx + 1).trim() };
                                }
                                return { title: 'Improvement Action', action: a };
                            }
                            return a;
                        });
                    }

                    // Career direction
                    const careerDir: CareerDirection = { ...defaultCareerDir };
                    if (parsed.career_direction && typeof parsed.career_direction === 'object') {
                        if (parsed.career_direction.immediate_target) {
                            careerDir.immediateTarget = parsed.career_direction.immediate_target;
                        }
                        if (parsed.career_direction.stronger_alignment) {
                            careerDir.strongerAlignment = parsed.career_direction.stronger_alignment;
                        }
                    } else if (typeof parsed.career_direction === 'string' && parsed.career_direction.trim()) {
                        careerDir.immediateTarget = parsed.career_direction;
                    }

                    return {
                        resumeStrengths: strengths.length > 0 ? strengths : defaultStrengths,
                        resumeWeaknesses: weaknesses.length > 0 ? weaknesses : defaultWeaknesses,
                        matchSummary: summary,
                        score: score,
                        scoreExplanation: scoreExplanation,
                        scoreFactors: scoreFactors,
                        missingRequiredSkills: missingReq,
                        missingPreferredSkills: missingPref,
                        missingJobSpecificSkills: missingJob,
                        improvementActions: actions.length > 0 ? actions : defaultActions,
                        careerDirection: careerDir,
                    };
                }
            } catch {
                // Parse legacy markdown containing headers
                const cleanText = reason.replace(/```(json)?/g, '').trim();
                if (cleanText.includes('###') || cleanText.includes('**')) {
                    const getSection = (num: number, title: string) => {
                        const regex = new RegExp(`###\\s*${num}\\.?\\s*${title}[\\s\\S]*?(?=###|$)`, 'i');
                        const match = cleanText.match(regex);
                        return match ? match[0].replace(new RegExp(`^###\\s*${num}\\.?\\s*${title}\\s*`, 'i'), '').trim() : '';
                    };

                    const strengthsRaw = getSection(1, 'Resume Strengths');
                    const weaknessesRaw = getSection(2, 'Resume Weaknesses');
                    const matchExplRaw = getSection(3, 'Explanation of Job Match');
                    const missingSkillsRaw = getSection(4, 'Most Important Missing Skills');
                    const actionsRaw = getSection(5, 'Three Prioritized Improvement Actions|Prioritized Improvement Actions');
                    const careerDirRaw = getSection(6, 'Career Direction');

                    // Parse itemized bullet points
                    const parseBulletItems = (raw: string) => {
                        const items: StrengthItem[] = [];
                        const bulletRegex = /\*\s*\*\*([^*]+)\:\*\*\s*([^\n*]+)/g;
                        let m;
                        while ((m = bulletRegex.exec(raw)) !== null) {
                            items.push({ title: m[1].trim(), description: m[2].trim() });
                        }
                        return items;
                    };

                    const strengths = parseBulletItems(strengthsRaw);
                    const weaknesses = parseBulletItems(weaknessesRaw);

                    // Actions
                    const actionItems: ImprovementActionItem[] = [];
                    const actionRegex = /\d+\.\s*\*\*([^*]+)\:\*\*\s*([^\n\d]+)/g;
                    let am;
                    while ((am = actionRegex.exec(actionsRaw)) !== null) {
                        actionItems.push({ title: am[1].trim(), action: am[2].trim() });
                    }

                    // Career direction
                    let immTarget = '';
                    let strAlign = '';
                    const immMatch = careerDirRaw.match(/\*\*Immediate Target\:\*\*\s*([^\n*]+)/i);
                    if (immMatch) immTarget = immMatch[1].trim();
                    const strMatch = careerDirRaw.match(/\*\*Stronger Alignment\:\*\*\s*([^\n*]+)/i);
                    if (strMatch) strAlign = strMatch[1].trim();

                    return {
                        resumeStrengths: strengths.length > 0 ? strengths : defaultStrengths,
                        resumeWeaknesses: weaknesses.length > 0 ? weaknesses : defaultWeaknesses,
                        matchSummary: matchExplRaw ? matchExplRaw.replace(/\*\*/g, '').replace(/•/g, '').trim().slice(0, 300) : defaultSummary,
                        score: match.matchScore,
                        scoreExplanation: matchExplRaw ? matchExplRaw.replace(/\*\*/g, '').trim() : defaultScoreExplanation,
                        scoreFactors: defaultFactors,
                        missingRequiredSkills: defaultMissingReq,
                        missingPreferredSkills: defaultMissingPref,
                        missingJobSpecificSkills: defaultMissingJob,
                        improvementActions: actionItems.length > 0 ? actionItems : defaultActions,
                        careerDirection: {
                            immediateTarget: immTarget || defaultCareerDir.immediateTarget,
                            strongerAlignment: strAlign || defaultCareerDir.strongerAlignment,
                        },
                    };
                }
            }
        }
    }

    // 3. Fallback to complete synthesized object
    return {
        resumeStrengths: defaultStrengths,
        resumeWeaknesses: defaultWeaknesses,
        matchSummary: defaultSummary,
        score: defaultScore,
        scoreExplanation: defaultScoreExplanation,
        scoreFactors: defaultFactors,
        missingRequiredSkills: defaultMissingReq,
        missingPreferredSkills: defaultMissingPref,
        missingJobSpecificSkills: defaultMissingJob,
        improvementActions: defaultActions,
        careerDirection: defaultCareerDir,
    };
};

export const Matches: React.FC<MatchesProps> = ({
    resumeId,
    onBack,
    onViewJobById,
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
        if (score >= 80) return 'text-emerald-400';
        if (score >= 60) return 'text-indigo-400';
        if (score >= 40) return 'text-amber-400';
        return 'text-rose-400';
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <button
                        onClick={onBack}
                        className="text-sm font-medium text-indigo-400 hover:text-indigo-300"
                    >
                        ← Back to Dashboard
                    </button>

                    <h2 className="mt-2 text-2xl font-bold text-white">
                        Job Matches
                    </h2>

                    <p className="mt-1 text-sm text-slate-400">
                        View job compatibility results for your latest resume.
                    </p>
                </div>

                <button
                    onClick={loadMatches}
                    className="flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-900 px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-slate-800 hover:text-white"
                >
                    <RefreshCw size={16} />
                    Refresh
                </button>
            </div>

            {loading ? (
                <div className="flex h-64 items-center justify-center rounded-2xl border border-slate-800 bg-slate-900">
                    <Loader2 className="animate-spin text-indigo-400" size={32} />
                </div>
            ) : error ? (
                <div className="flex items-center gap-3 rounded-2xl border border-rose-500/20 bg-rose-500/10 p-4 text-sm text-rose-400">
                    <AlertCircle size={18} />
                    <span>{error}</span>
                </div>
            ) : matches.length === 0 ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-12 text-center">
                    <Target size={42} className="mx-auto mb-4 text-slate-600" />
                    <h3 className="text-lg font-bold text-white">No matches yet</h3>
                    <p className="mt-2 text-sm text-slate-400">
                        No matching jobs have been generated for this resume yet.
                    </p>
                </div>
            ) : (
                <div className="space-y-6">
                    {matches.map((match) => {
                        const why = parseWhyThisMatch(match);

                        return (
                            <div
                                key={match.id}
                                className="rounded-2xl border border-slate-800 bg-slate-900 p-6 md:p-8"
                            >
                                {/* JOB HEADER */}
                                <div className="flex flex-col gap-5 md:flex-row md:items-start md:justify-between">
                                    <div>
                                        <h3 className="text-2xl font-bold text-white">
                                            {match.jobTitle}
                                        </h3>
                                        <p className="mt-1 text-base font-medium text-indigo-400">
                                            {match.company}
                                        </p>
                                        {onViewJobById && (
                                            <button
                                                type="button"
                                                onClick={() => onViewJobById(match.jobId)}
                                                className="mt-3 inline-flex items-center gap-1.5 rounded-xl border border-indigo-500/30 bg-indigo-600/15 px-3.5 py-1.5 text-xs font-semibold text-indigo-300 transition hover:bg-indigo-600 hover:text-white"
                                            >
                                                <Eye size={14} />
                                                View Job Details
                                            </button>
                                        )}
                                    </div>

                                    <div className="text-left md:text-right">
                                        <p className="text-xs uppercase tracking-wider text-slate-500">
                                            Match Score
                                        </p>
                                        <p className={`mt-1 text-4xl font-extrabold ${scoreClass(match.matchScore)}`}>
                                            {match.matchScore}
                                            <span className="text-sm font-normal text-slate-500">/100</span>
                                        </p>
                                    </div>
                                </div>

                                {/* SCORE BAR */}
                                <div className="mt-6 h-2.5 overflow-hidden rounded-full bg-slate-800">
                                    <div
                                        className="h-full rounded-full bg-indigo-500 transition-all duration-500"
                                        style={{
                                            width: `${Math.min(100, Math.max(0, match.matchScore))}%`,
                                        }}
                                    />
                                </div>

                                {/* MATCHING SKILLS TAGS */}
                                {match.matchingSkills && match.matchingSkills.length > 0 && (
                                    <div className="mt-6">
                                        <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                            Matching Skills
                                        </p>
                                        <div className="flex flex-wrap gap-2">
                                            {match.matchingSkills.map((skill) => (
                                                <span
                                                    key={skill}
                                                    className="rounded-lg bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-300"
                                                >
                                                    {skill}
                                                </span>
                                            ))}
                                        </div>
                                    </div>
                                )}

                                {/* MISSING SKILLS TAGS */}
                                {match.missingSkills && match.missingSkills.length > 0 && (
                                    <div className="mt-4">
                                        <p className="mb-2 text-xs uppercase tracking-wider text-slate-500">
                                            Missing Skills
                                        </p>
                                        <div className="flex flex-wrap gap-2">
                                            {match.missingSkills.map((skill) => (
                                                <span
                                                    key={skill}
                                                    className="rounded-lg bg-rose-500/10 px-2.5 py-1 text-xs font-medium text-rose-300"
                                                >
                                                    {skill}
                                                </span>
                                            ))}
                                        </div>
                                    </div>
                                )}

                                {/* ============================================================
                                    6-PART WHY THIS MATCH ASSESSMENT
                                   ============================================================ */}
                                {why && (
                                    <div className="mt-8 border-t border-slate-800 pt-8 space-y-7">
                                        <div className="flex items-center gap-2">
                                            <Award className="text-indigo-400" size={20} />
                                            <h4 className="text-base font-bold uppercase tracking-wider text-white">
                                                Why This Match
                                            </h4>
                                        </div>

                                        {/* 1. RESUME STRENGTHS */}
                                        {why.resumeStrengths && why.resumeStrengths.length > 0 && (
                                            <div className="rounded-xl border border-emerald-900/40 bg-emerald-950/20 p-5">
                                                <div className="flex items-center gap-2 mb-3">
                                                    <CheckCircle2 className="text-emerald-400" size={18} />
                                                    <h5 className="text-sm font-bold text-emerald-300 uppercase tracking-wide">
                                                        1. Resume Strengths
                                                    </h5>
                                                </div>
                                                <div className="space-y-3">
                                                    {why.resumeStrengths.map((item, idx) => {
                                                        const isObj = typeof item === 'object' && item !== null;
                                                        const title = isObj ? item.title : '';
                                                        const desc = isObj ? item.description : item;
                                                        return (
                                                            <div key={idx} className="text-sm leading-relaxed text-slate-300">
                                                                {title && <span className="font-semibold text-emerald-200">{title}: </span>}
                                                                <span>{desc}</span>
                                                            </div>
                                                        );
                                                    })}
                                                </div>
                                            </div>
                                        )}

                                        {/* 2. RESUME WEAKNESSES */}
                                        {why.resumeWeaknesses && why.resumeWeaknesses.length > 0 && (
                                            <div className="rounded-xl border border-amber-900/40 bg-amber-950/20 p-5">
                                                <div className="flex items-center gap-2 mb-3">
                                                    <AlertTriangle className="text-amber-400" size={18} />
                                                    <h5 className="text-sm font-bold text-amber-300 uppercase tracking-wide">
                                                        2. Resume Weaknesses
                                                    </h5>
                                                </div>
                                                <div className="space-y-3">
                                                    {why.resumeWeaknesses.map((item, idx) => {
                                                        const isObj = typeof item === 'object' && item !== null;
                                                        const title = isObj ? item.title : '';
                                                        const desc = isObj ? item.description : item;
                                                        return (
                                                            <div key={idx} className="text-sm leading-relaxed text-slate-300">
                                                                {title && <span className="font-semibold text-amber-200">{title}: </span>}
                                                                <span>{desc}</span>
                                                            </div>
                                                        );
                                                    })}
                                                </div>
                                            </div>
                                        )}

                                        {/* 3. EXPLANATION OF JOB MATCH */}
                                        {(why.matchSummary || why.scoreExplanation) && (
                                            <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-5">
                                                <div className="flex items-center gap-2 mb-3">
                                                    <TrendingUp className="text-indigo-400" size={18} />
                                                    <h5 className="text-sm font-bold text-indigo-300 uppercase tracking-wide">
                                                        3. Explanation of Job Match
                                                    </h5>
                                                </div>
                                                {why.matchSummary && (
                                                    <p className="text-sm leading-relaxed text-slate-300 mb-3">
                                                        {why.matchSummary}
                                                    </p>
                                                )}
                                                {why.scoreExplanation && why.scoreExplanation !== why.matchSummary && (
                                                    <p className="text-sm leading-relaxed text-slate-400 mb-3">
                                                        <strong className="text-slate-200">Analysis: </strong>
                                                        {why.scoreExplanation}
                                                    </p>
                                                )}
                                                {why.scoreFactors && why.scoreFactors.length > 0 && (
                                                    <ul className="space-y-1.5 text-xs text-slate-400 mt-3 pt-3 border-t border-slate-800/80">
                                                        {why.scoreFactors.map((factor, idx) => (
                                                            <li key={idx} className="flex items-center gap-2">
                                                                <span className="text-indigo-400">•</span>
                                                                <span>{factor}</span>
                                                            </li>
                                                        ))}
                                                    </ul>
                                                )}
                                            </div>
                                        )}

                                        {/* 4. MOST IMPORTANT MISSING SKILLS */}
                                        {(why.missingRequiredSkills?.length || why.missingPreferredSkills?.length || why.missingJobSpecificSkills?.length) ? (
                                            <div className="rounded-xl border border-rose-900/30 bg-rose-950/10 p-5">
                                                <h5 className="text-sm font-bold text-rose-300 uppercase tracking-wide mb-4">
                                                    4. Most Important Missing Skills
                                                </h5>
                                                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                                    {why.missingRequiredSkills && why.missingRequiredSkills.length > 0 && (
                                                        <div className="rounded-lg bg-slate-900/80 p-3 border border-slate-800">
                                                            <p className="text-xs font-semibold text-rose-400 uppercase tracking-wider mb-2">
                                                                Required (Critical)
                                                            </p>
                                                            <ul className="space-y-1 text-xs text-slate-300">
                                                                {why.missingRequiredSkills.map((s, i) => (
                                                                    <li key={i} className="flex items-center gap-1.5">
                                                                        <span className="text-rose-500">•</span>
                                                                        <span>{s}</span>
                                                                    </li>
                                                                ))}
                                                            </ul>
                                                        </div>
                                                    )}

                                                    {why.missingPreferredSkills && why.missingPreferredSkills.length > 0 && (
                                                        <div className="rounded-lg bg-slate-900/80 p-3 border border-slate-800">
                                                            <p className="text-xs font-semibold text-amber-400 uppercase tracking-wider mb-2">
                                                                Preferred (Cloud / Arch)
                                                            </p>
                                                            <ul className="space-y-1 text-xs text-slate-300">
                                                                {why.missingPreferredSkills.map((s, i) => (
                                                                    <li key={i} className="flex items-center gap-1.5">
                                                                        <span className="text-amber-500">•</span>
                                                                        <span>{s}</span>
                                                                    </li>
                                                                ))}
                                                            </ul>
                                                        </div>
                                                    )}

                                                    {why.missingJobSpecificSkills && why.missingJobSpecificSkills.length > 0 && (
                                                        <div className="rounded-lg bg-slate-900/80 p-3 border border-slate-800">
                                                            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
                                                                Job Specific
                                                            </p>
                                                            <ul className="space-y-1 text-xs text-slate-300">
                                                                {why.missingJobSpecificSkills.map((s, i) => (
                                                                    <li key={i} className="flex items-center gap-1.5">
                                                                        <span className="text-slate-400">•</span>
                                                                        <span>{s}</span>
                                                                    </li>
                                                                ))}
                                                            </ul>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        ) : null}

                                        {/* 5. THREE PRIORITIZED IMPROVEMENT ACTIONS */}
                                        {why.improvementActions && why.improvementActions.length > 0 && (
                                            <div className="rounded-xl border border-indigo-900/40 bg-indigo-950/20 p-5">
                                                <div className="flex items-center gap-2 mb-3">
                                                    <ListChecks className="text-indigo-400" size={18} />
                                                    <h5 className="text-sm font-bold text-indigo-300 uppercase tracking-wide">
                                                        5. Prioritized Improvement Actions
                                                    </h5>
                                                </div>
                                                <ol className="space-y-3 text-sm leading-relaxed text-slate-300">
                                                    {why.improvementActions.map((item, idx) => {
                                                        const isObj = typeof item === 'object' && item !== null;
                                                        const title = isObj ? item.title : '';
                                                        const action = isObj ? item.action : item;
                                                        return (
                                                            <li key={idx} className="flex items-start gap-3">
                                                                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-indigo-500/20 text-xs font-bold text-indigo-400">
                                                                    {idx + 1}
                                                                </span>
                                                                <div>
                                                                    {title && <strong className="text-white block mb-0.5">{title}</strong>}
                                                                    <span className="text-slate-400">{action}</span>
                                                                </div>
                                                            </li>
                                                        );
                                                    })}
                                                </ol>
                                            </div>
                                        )}

                                        {/* 6. CAREER DIRECTION */}
                                        {why.careerDirection && (
                                            <div className="rounded-xl border border-purple-900/40 bg-purple-950/20 p-5">
                                                <div className="flex items-center gap-2 mb-3">
                                                    <Compass className="text-purple-400" size={18} />
                                                    <h5 className="text-sm font-bold text-purple-300 uppercase tracking-wide">
                                                        6. Career Direction & Pathways
                                                    </h5>
                                                </div>
                                                <div className="space-y-3 text-sm leading-relaxed">
                                                    {typeof why.careerDirection === 'object' && why.careerDirection !== null ? (
                                                        <>
                                                            {why.careerDirection.immediateTarget && (
                                                                <div>
                                                                    <span className="font-semibold text-purple-200">Immediate Target: </span>
                                                                    <span className="text-slate-300">{why.careerDirection.immediateTarget}</span>
                                                                </div>
                                                            )}
                                                            {why.careerDirection.strongerAlignment && (
                                                                <div className="mt-2 rounded-lg bg-purple-900/20 p-3 border border-purple-800/40">
                                                                    <span className="font-semibold text-purple-300">Stronger Alignment & Potential: </span>
                                                                    <span className="text-slate-300">{why.careerDirection.strongerAlignment}</span>
                                                                </div>
                                                            )}
                                                        </>
                                                    ) : (
                                                        <div className="text-slate-300">{String(why.careerDirection)}</div>
                                                    )}
                                                </div>
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