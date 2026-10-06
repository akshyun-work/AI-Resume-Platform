import React, { useEffect, useState } from 'react';
import {
    ArrowLeft,
    UserCircle,
    Mail,
    Phone,
    ShieldCheck,
    Camera,
    CheckCircle2,
    AlertCircle,
    FileText,
    Eye,
    Download,
    History,
    Loader2,
    Award,
    Sparkles,
    Trash2,
    Calendar,
    Clock,
} from 'lucide-react';

import { FaceAuthModal } from './FaceAuthModal';
import { ResumePdfModal } from './ResumePdfModal';
import { apiRequest, downloadResumePdf } from '../services/api';
import type { ApiResponse, AtsAnalysis, Candidate, Resume } from '../types/api';

interface ProfileProps {
    candidate: Candidate;
    onBack: () => void;
}

export const Profile: React.FC<ProfileProps> = ({
    candidate,
    onBack,
}) => {
    const [showFaceModal, setShowFaceModal] = useState(false);
    const [faceRegistered, setFaceRegistered] = useState<boolean>(Boolean(candidate.hasFaceRegistered));
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [removingFace, setRemovingFace] = useState(false);

    const [resumes, setResumes] = useState<Resume[]>([]);
    const [resumesLoading, setResumesLoading] = useState<boolean>(true);
    const [latestAtsScore, setLatestAtsScore] = useState<number | null>(null);
    const [previewResumeId, setPreviewResumeId] = useState<string | null>(null);
    const [downloadingId, setDownloadingId] = useState<string | null>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [confirmDeleteResume, setConfirmDeleteResume] = useState<Resume | null>(null);

    useEffect(() => {
        const fetchCandidateAndResumes = async () => {
            try {
                setResumesLoading(true);
                const [resumeRes, candidateRes, atsRes] = await Promise.allSettled([
                    apiRequest<ApiResponse<Resume[]>>('/api/resumes'),
                    apiRequest<ApiResponse<Candidate>>('/api/candidates/me'),
                    apiRequest<ApiResponse<AtsAnalysis[]>>('/api/ats'),
                ]);

                if (resumeRes.status === 'fulfilled' && resumeRes.value.success && resumeRes.value.data) {
                    setResumes(resumeRes.value.data);
                }

                if (candidateRes.status === 'fulfilled' && candidateRes.value.success && candidateRes.value.data) {
                    if (typeof candidateRes.value.data.hasFaceRegistered === 'boolean') {
                        setFaceRegistered(candidateRes.value.data.hasFaceRegistered);
                    }
                }

                if (atsRes.status === 'fulfilled' && atsRes.value.success && atsRes.value.data && atsRes.value.data.length > 0) {
                    // Sort descending by analyzedAt if multiple analyses exist
                    const sorted = [...atsRes.value.data].sort(
                        (a, b) => new Date(b.analyzedAt).getTime() - new Date(a.analyzedAt).getTime()
                    );
                    setLatestAtsScore(sorted[0].overallScore);
                }
            } catch (err) {
                console.error('Failed to load profile data:', err);
            } finally {
                setResumesLoading(false);
            }
        };

        fetchCandidateAndResumes();
    }, []);

    const handleFaceRegistered = () => {
        setFaceRegistered(true);
        setMessage('Face ID registered successfully.');
        setError(null);
        setShowFaceModal(false);
    };

    const handleRemoveFace = async () => {
        try {
            setRemovingFace(true);
            setError(null);
            const res = await apiRequest<ApiResponse<object>>('/api/candidates/me/face', {
                method: 'DELETE',
            });
            if (res.success) {
                setFaceRegistered(false);
                setMessage('Face ID removed successfully.');
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to remove Face ID.');
        } finally {
            setRemovingFace(false);
        }
    };

    const handleFaceError = (errorMessage: string) => {
        setError(errorMessage);
        setMessage(null);
    };

    const handleDownload = async (r: Resume) => {
        try {
            setDownloadingId(r.id);
            await downloadResumePdf(
                r.id,
                r.originalFileName || `resume-v${r.versionNumber}.pdf`
            );
        } catch (err) {
            console.error('Download failed:', err);
            setError('Failed to download resume PDF.');
        } finally {
            setDownloadingId(null);
        }
    };

    const handleDeleteResume = async (resume: Resume) => {
        try {
            setDeletingId(resume.id);
            setError(null);
            setMessage(null);

            const res = await apiRequest<ApiResponse<object>>(`/api/resumes/${resume.id}`, {
                method: 'DELETE',
            });

            if (res.success) {
                const remaining = resumes.filter((x) => x.id !== resume.id);
                // If deleted resume was latest and versions remain, mark newest remaining as latest
                if (resume.isLatest && remaining.length > 0) {
                    remaining[0].isLatest = true;
                }
                setResumes(remaining);
                setConfirmDeleteResume(null);
                setMessage(`Resume "${resume.originalFileName}" (v${resume.versionNumber}) deleted successfully.`);

                // Refresh ATS score
                try {
                    const atsRes = await apiRequest<ApiResponse<AtsAnalysis[]>>('/api/ats');
                    if (atsRes.success && atsRes.data && atsRes.data.length > 0) {
                        const sorted = [...atsRes.data].sort(
                            (a, b) => new Date(b.analyzedAt).getTime() - new Date(a.analyzedAt).getTime()
                        );
                        setLatestAtsScore(sorted[0].overallScore);
                    } else {
                        setLatestAtsScore(null);
                    }
                } catch {
                    // Ignore ATS refresh error
                }
            } else {
                throw new Error(res.message || 'Failed to delete resume.');
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to delete resume.');
            setConfirmDeleteResume(null);
        } finally {
            setDeletingId(null);
        }
    };

    return (
        <div className="mx-auto w-full max-w-4xl space-y-6">
            {/* Header */}
            <div className="flex items-center gap-4">
                <button
                    type="button"
                    onClick={onBack}
                    className="flex h-10 w-10 items-center justify-center rounded-xl border border-slate-700 bg-slate-900 text-slate-300 transition hover:border-slate-600 hover:bg-slate-800 hover:text-white"
                >
                    <ArrowLeft size={18} />
                </button>

                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Profile & Security
                    </h2>

                    <p className="mt-1 text-sm text-slate-400">
                        Manage your account and optional authentication methods.
                    </p>
                </div>
            </div>

            {/* Success message */}
            {message && (
                <div className="flex items-center gap-3 rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-4 text-sm text-emerald-300">
                    <CheckCircle2 size={18} />
                    <span>{message}</span>
                </div>
            )}

            {/* Error message */}
            {error && (
                <div className="flex items-center gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                    <AlertCircle size={18} />
                    <span>{error}</span>
                </div>
            )}

            {/* Personal Information */}
            <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                <div className="mb-6 flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-600/15">
                        <UserCircle
                            size={22}
                            className="text-indigo-400"
                        />
                    </div>

                    <div>
                        <h3 className="font-bold text-white">
                            Personal Information
                        </h3>

                        <p className="text-xs text-slate-500">
                            Your registered candidate information.
                        </p>
                    </div>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                    {/* Full name */}
                    <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-slate-500">
                            <UserCircle size={15} />
                            Full Name
                        </div>

                        <p className="mt-2 font-semibold text-white">
                            {candidate.fullName}
                        </p>
                    </div>

                    {/* Email */}
                    <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-slate-500">
                            <Mail size={15} />
                            Email
                        </div>

                        <p className="mt-2 font-semibold text-white break-all">
                            {candidate.email}
                        </p>
                    </div>

                    {/* Phone */}
                    <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-slate-500">
                            <Phone size={15} />
                            Phone
                        </div>

                        <p className="mt-2 font-semibold text-white">
                            {candidate.phone || 'Not provided'}
                        </p>
                    </div>

                    {/* Latest ATS Score */}
                    <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-slate-500">
                            <Award size={15} className="text-amber-400" />
                            Latest ATS Score
                        </div>

                        {latestAtsScore !== null ? (
                            <div className="mt-2 flex items-center gap-3">
                                <span className="text-xl font-extrabold text-white">
                                    {latestAtsScore}
                                    <span className="text-xs font-normal text-slate-400 ml-1">/ 100</span>
                                </span>
                                <span
                                    className={`rounded-lg px-2.5 py-0.5 text-xs font-semibold border ${
                                        latestAtsScore >= 80
                                            ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-400'
                                            : latestAtsScore >= 60
                                            ? 'border-amber-500/30 bg-amber-500/10 text-amber-400'
                                            : 'border-rose-500/30 bg-rose-500/10 text-rose-400'
                                    }`}
                                >
                                    {latestAtsScore >= 80
                                        ? 'Strong Match'
                                        : latestAtsScore >= 60
                                        ? 'Good Match'
                                        : 'Needs Improvement'}
                                </span>
                            </div>
                        ) : (
                            <p className="mt-2 text-sm text-slate-400">
                                Not analyzed yet
                            </p>
                        )}
                    </div>
                </div>
            </section>

            {/* Resume History & Versions */}
            <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                <div className="mb-6 flex items-center justify-between gap-4">
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-600/15">
                            <History
                                size={22}
                                className="text-indigo-400"
                            />
                        </div>

                        <div>
                            <h3 className="font-bold text-white">
                                Resume Versions & Documents
                            </h3>

                            <p className="text-xs text-slate-500">
                                View and download all uploaded versions of your resume.
                            </p>
                        </div>
                    </div>

                    <span className="rounded-lg bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-400">
                        {resumes.length} {resumes.length === 1 ? 'Version' : 'Versions'}
                    </span>
                </div>

                {resumesLoading ? (
                    <div className="flex items-center justify-center p-8">
                        <Loader2 size={24} className="animate-spin text-indigo-400" />
                    </div>
                ) : resumes.length === 0 ? (
                    <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-6 text-center">
                        <FileText size={32} className="mx-auto mb-2 text-slate-600" />
                        <p className="text-sm text-slate-400">No resumes uploaded yet.</p>
                    </div>
                ) : (
                    <div className="space-y-3">
                        {resumes.map((r) => (
                            <div
                                key={r.id}
                                className="flex flex-col gap-4 rounded-xl border border-slate-800 bg-slate-950/60 p-4 sm:flex-row sm:items-center sm:justify-between transition hover:border-indigo-500/30"
                            >
                                <div className="flex items-center gap-3 min-w-0">
                                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-indigo-500/10 text-indigo-400">
                                        <FileText size={20} />
                                    </div>

                                    <div className="min-w-0">
                                        <div className="flex items-center gap-2">
                                            <p className="font-semibold text-white truncate">
                                                {r.originalFileName}
                                            </p>

                                            <span
                                                className={`rounded-md px-2 py-0.5 text-xs font-semibold ${
                                                    r.isLatest
                                                        ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                                                        : 'bg-slate-800 text-slate-400'
                                                }`}
                                            >
                                                v{r.versionNumber} {r.isLatest ? '(Current)' : ''}
                                            </span>
                                        </div>

                                        <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                            <span className="flex items-center gap-1.5 text-slate-400">
                                                <Calendar size={13} className="text-slate-500 shrink-0" />
                                                Uploaded on {new Date(r.createdAt).toLocaleDateString(undefined, {
                                                    month: 'short',
                                                    day: 'numeric',
                                                    year: 'numeric',
                                                    hour: '2-digit',
                                                    minute: '2-digit',
                                                })}
                                            </span>
                                            {r.fileSizeBytes ? (
                                                <span className="text-slate-500">• {(r.fileSizeBytes / 1024).toFixed(1)} KB</span>
                                            ) : null}
                                        </div>
                                    </div>
                                </div>

                                <div className="flex items-center gap-2 shrink-0">
                                    <button
                                        type="button"
                                        onClick={() => setPreviewResumeId(r.id)}
                                        className="flex items-center gap-1.5 rounded-xl bg-indigo-600/15 border border-indigo-500/30 px-3.5 py-2 text-xs font-semibold text-indigo-300 transition hover:bg-indigo-600 hover:text-white"
                                    >
                                        <Eye size={14} />
                                        View PDF
                                    </button>

                                    <button
                                        type="button"
                                        onClick={() => handleDownload(r)}
                                        disabled={downloadingId === r.id}
                                        className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800 px-3.5 py-2 text-xs font-semibold text-slate-300 transition hover:bg-slate-700 hover:text-white disabled:opacity-50"
                                    >
                                        {downloadingId === r.id ? (
                                            <Loader2 size={14} className="animate-spin" />
                                        ) : (
                                            <Download size={14} />
                                        )}
                                        Download
                                    </button>

                                    <button
                                        type="button"
                                        onClick={() => setConfirmDeleteResume(r)}
                                        disabled={resumes.length <= 1 || deletingId === r.id}
                                        title={
                                            resumes.length <= 1
                                                ? 'At least one active resume is required on your profile'
                                                : 'Delete this resume version'
                                        }
                                        className={`flex items-center gap-1.5 rounded-xl border border-rose-500/30 bg-rose-500/10 px-3.5 py-2 text-xs font-semibold text-rose-400 transition ${
                                            resumes.length <= 1
                                                ? 'opacity-40 cursor-not-allowed'
                                                : 'hover:bg-rose-600 hover:text-white'
                                        } disabled:opacity-40`}
                                    >
                                        {deletingId === r.id ? (
                                            <Loader2 size={14} className="animate-spin" />
                                        ) : (
                                            <Trash2 size={14} />
                                        )}
                                        Delete
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            {/* Authentication */}
            <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                <div className="mb-6 flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-600/15">
                        <ShieldCheck
                            size={22}
                            className="text-emerald-400"
                        />
                    </div>

                    <div>
                        <h3 className="font-bold text-white">
                            Authentication
                        </h3>

                        <p className="text-xs text-slate-500">
                            Choose how you authenticate with the platform.
                        </p>
                    </div>
                </div>

                {/* Email/password */}
                <div className="flex flex-col gap-4 rounded-xl border border-slate-800 bg-slate-950/60 p-5 md:flex-row md:items-center md:justify-between">
                    <div>
                        <div className="flex items-center gap-2">
                            <Mail
                                size={18}
                                className="text-indigo-400"
                            />

                            <h4 className="font-semibold text-white">
                                Email & Password
                            </h4>
                        </div>

                        <p className="mt-1 text-sm text-slate-500">
                            Primary authentication method
                        </p>
                    </div>

                    <span className="flex items-center gap-2 rounded-lg border border-emerald-500/20 bg-emerald-500/10 px-3 py-2 text-xs font-semibold text-emerald-400">
                        <CheckCircle2 size={15} />
                        Enabled
                    </span>
                </div>

                {/* Face ID */}
                <div className="mt-4 flex flex-col gap-5 rounded-xl border border-slate-800 bg-slate-950/60 p-5 md:flex-row md:items-center md:justify-between">
                    <div>
                        <div className="flex items-center gap-2">
                            <Camera
                                size={18}
                                className="text-purple-400"
                            />

                            <h4 className="font-semibold text-white">
                                Face ID
                            </h4>
                        </div>

                        <p className="mt-1 text-sm text-slate-500">
                            Optional biometric authentication for instant passwordless sign in.
                        </p>

                        <div className="mt-3">
                            {faceRegistered ? (
                                <span className="inline-flex items-center gap-1.5 rounded-lg border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-400">
                                    <CheckCircle2 size={14} />
                                    Face ID Active & Registered
                                </span>
                            ) : (
                                <span className="inline-flex items-center gap-1.5 rounded-lg border border-slate-700 bg-slate-800 px-3 py-1 text-xs font-medium text-slate-400">
                                    Not Registered
                                </span>
                            )}
                        </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                        {faceRegistered && (
                            <button
                                type="button"
                                onClick={handleRemoveFace}
                                disabled={removingFace}
                                className="rounded-xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm font-semibold text-rose-300 transition hover:bg-rose-500/20 disabled:opacity-50"
                            >
                                {removingFace ? 'Removing...' : 'Remove Face ID'}
                            </button>
                        )}

                        <button
                            type="button"
                            onClick={() => {
                                setError(null);
                                setMessage(null);
                                setShowFaceModal(true);
                            }}
                            className="flex shrink-0 items-center justify-center gap-2 rounded-xl bg-indigo-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-indigo-500"
                        >
                            <Camera size={17} />

                            {faceRegistered
                                ? 'Re-register / Update'
                                : 'Register Face ID'}
                        </button>
                    </div>
                </div>
            </section>

            {/* Explanation */}
            <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-5">
                <p className="text-sm leading-6 text-slate-400">
                    Face ID is optional. You can use the complete resume
                    upload and ATS analysis functionality with your normal
                    email and password login.
                </p>
            </div>

            {/* Face registration modal */}
            {showFaceModal && (
                <FaceAuthModal
                    mode="register"
                    candidateId={candidate.id}
                    onSuccess={handleFaceRegistered}
                    onClose={() => setShowFaceModal(false)}
                    onError={handleFaceError}
                />
            )}

            {/* Resume PDF Preview Modal */}
            {previewResumeId && (
                <ResumePdfModal
                    initialResumeId={previewResumeId}
                    availableResumes={resumes}
                    onClose={() => setPreviewResumeId(null)}
                />
            )}

            {/* Delete Resume Confirmation Modal */}
            {confirmDeleteResume && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-sm animate-in fade-in duration-150">
                    <div className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
                        <div className="flex items-center gap-3">
                            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-rose-500/10 border border-rose-500/20 text-rose-400">
                                <Trash2 size={24} />
                            </div>
                            <div>
                                <h3 className="text-lg font-bold text-white">Delete Resume Version</h3>
                                <p className="text-xs text-slate-400">
                                    v{confirmDeleteResume.versionNumber} {confirmDeleteResume.isLatest ? '(Current)' : ''} • {confirmDeleteResume.originalFileName}
                                </p>
                            </div>
                        </div>

                        <p className="mt-4 text-sm leading-relaxed text-slate-300">
                            Are you sure you want to delete this resume version? This will permanently delete the uploaded PDF document and its ATS analysis history.
                        </p>

                        <div className="mt-6 flex items-center justify-end gap-3">
                            <button
                                type="button"
                                onClick={() => setConfirmDeleteResume(null)}
                                disabled={Boolean(deletingId)}
                                className="rounded-xl border border-slate-700 bg-slate-800 px-4 py-2.5 text-sm font-semibold text-slate-300 hover:bg-slate-700 hover:text-white transition"
                            >
                                Cancel
                            </button>

                            <button
                                type="button"
                                onClick={() => handleDeleteResume(confirmDeleteResume)}
                                disabled={Boolean(deletingId)}
                                className="flex items-center gap-2 rounded-xl bg-rose-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-rose-500 transition disabled:opacity-50"
                            >
                                {deletingId ? (
                                    <>
                                        <Loader2 size={16} className="animate-spin" />
                                        Deleting...
                                    </>
                                ) : (
                                    <>
                                        <Trash2 size={16} />
                                        Delete Resume
                                    </>
                                )}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};