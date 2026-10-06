import React, { useEffect, useState } from 'react';
import {
    X,
    Download,
    ExternalLink,
    Loader2,
    AlertCircle,
    FileText,
    History,
    RefreshCw,
} from 'lucide-react';

import { fetchResumeBlob, downloadResumePdf, apiRequest } from '../services/api';
import type { ApiResponse, Resume } from '../types/api';

interface ResumePdfModalProps {
    initialResumeId: string;
    onClose: () => void;
    availableResumes?: Resume[];
}

export const ResumePdfModal: React.FC<ResumePdfModalProps> = ({
    initialResumeId,
    onClose,
    availableResumes,
}) => {
    const [resumes, setResumes] = useState<Resume[]>(availableResumes || []);
    const [selectedResumeId, setSelectedResumeId] = useState<string>(initialResumeId);
    const [pdfUrl, setPdfUrl] = useState<string | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [downloading, setDownloading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);

    // If availableResumes was not supplied, fetch all versions for the candidate
    useEffect(() => {
        if (!availableResumes || availableResumes.length === 0) {
            apiRequest<ApiResponse<Resume[]>>('/api/resumes')
                .then((res) => {
                    if (res.success && res.data) {
                        setResumes(res.data);
                    }
                })
                .catch((err) => {
                    console.error('Failed to fetch resume versions:', err);
                });
        }
    }, [availableResumes]);

    // Load selected resume PDF
    useEffect(() => {
        let objectUrl: string | null = null;
        let isMounted = true;

        const loadPdf = async () => {
            if (!selectedResumeId) return;

            try {
                setLoading(true);
                setError(null);

                const blob = await fetchResumeBlob(selectedResumeId);
                if (!isMounted) return;

                objectUrl = URL.createObjectURL(blob);
                setPdfUrl(objectUrl);
            } catch (err) {
                if (!isMounted) return;
                console.error(err);
                setError(
                    err instanceof Error
                        ? err.message
                        : 'Unable to load resume PDF preview.'
                );
            } finally {
                if (isMounted) {
                    setLoading(false);
                }
            }
        };

        loadPdf();

        return () => {
            isMounted = false;
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
        };
    }, [selectedResumeId]);

    const activeResume =
        resumes.find((r) => r.id === selectedResumeId) ||
        resumes[0] ||
        null;

    const handleDownload = async () => {
        if (!activeResume) return;
        try {
            setDownloading(true);
            await downloadResumePdf(
                activeResume.id,
                activeResume.originalFileName || `resume-v${activeResume.versionNumber}.pdf`
            );
        } catch (err) {
            console.error(err);
        } finally {
            setDownloading(false);
        }
    };

    const handleOpenInNewTab = () => {
        if (pdfUrl) {
            window.open(pdfUrl, '_blank');
        }
    };

    // Close on Escape key
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') {
                onClose();
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [onClose]);

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4 backdrop-blur-sm sm:p-6">
            <div className="flex h-[92vh] w-full max-w-5xl flex-col rounded-2xl border border-slate-800 bg-slate-900 shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
                {/* Modal Header */}
                <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-800 bg-slate-950/80 px-6 py-4">
                    <div className="flex items-center gap-3 min-w-0">
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-indigo-500/10 text-indigo-400">
                            <FileText size={20} />
                        </div>
                        <div className="min-w-0">
                            <div className="flex items-center gap-2">
                                <h3 className="truncate text-base font-bold text-white">
                                    {activeResume?.originalFileName || 'Resume Preview'}
                                </h3>
                                {activeResume && (
                                    <span
                                        className={`rounded-md px-2 py-0.5 text-xs font-semibold ${
                                            activeResume.isLatest
                                                ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                                                : 'bg-slate-800 text-slate-400'
                                        }`}
                                    >
                                        v{activeResume.versionNumber} {activeResume.isLatest ? '(Latest)' : ''}
                                    </span>
                                )}
                            </div>
                            <p className="text-xs text-slate-500 truncate">
                                {activeResume?.createdAt
                                    ? `Uploaded ${new Date(activeResume.createdAt).toLocaleDateString(undefined, {
                                          month: 'short',
                                          day: 'numeric',
                                          year: 'numeric',
                                          hour: '2-digit',
                                          minute: '2-digit',
                                      })}`
                                    : 'Resume Document'}
                                {activeResume?.fileSizeBytes
                                    ? ` • ${(activeResume.fileSizeBytes / 1024).toFixed(1)} KB`
                                    : ''}
                            </p>
                        </div>
                    </div>

                    {/* Version Switcher */}
                    {resumes.length > 1 && (
                        <div className="flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-900/90 p-1">
                            <span className="flex items-center gap-1 px-2 text-xs font-medium text-slate-400">
                                <History size={13} />
                                Versions:
                            </span>
                            <div className="flex gap-1">
                                {resumes.map((r) => {
                                    const isSelected = r.id === selectedResumeId;
                                    return (
                                        <button
                                            key={r.id}
                                            type="button"
                                            onClick={() => setSelectedResumeId(r.id)}
                                            className={`rounded-lg px-2.5 py-1 text-xs font-medium transition ${
                                                isSelected
                                                    ? 'bg-indigo-600 text-white shadow-sm'
                                                    : 'text-slate-400 hover:bg-slate-800 hover:text-white'
                                            }`}
                                        >
                                            v{r.versionNumber}
                                            {r.isLatest ? ' ★' : ''}
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                    )}

                    {/* Actions */}
                    <div className="flex items-center gap-2">
                        {pdfUrl && (
                            <button
                                type="button"
                                onClick={handleOpenInNewTab}
                                title="Open PDF in new tab"
                                className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800 px-3 py-2 text-xs font-semibold text-slate-300 transition hover:bg-slate-700 hover:text-white"
                            >
                                <ExternalLink size={14} />
                                <span className="hidden sm:inline">New Tab</span>
                            </button>
                        )}

                        <button
                            type="button"
                            onClick={handleDownload}
                            disabled={downloading || !activeResume}
                            title="Download PDF"
                            className="flex items-center gap-1.5 rounded-xl bg-indigo-600 px-3.5 py-2 text-xs font-semibold text-white transition hover:bg-indigo-500 disabled:opacity-50"
                        >
                            {downloading ? (
                                <Loader2 size={14} className="animate-spin" />
                            ) : (
                                <Download size={14} />
                            )}
                            <span className="hidden sm:inline">Download</span>
                        </button>

                        <button
                            type="button"
                            onClick={onClose}
                            title="Close viewer"
                            className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-700 bg-slate-800 text-slate-400 transition hover:bg-slate-700 hover:text-white"
                        >
                            <X size={18} />
                        </button>
                    </div>
                </div>

                {/* Viewer Body */}
                <div className="relative flex-1 bg-slate-950 p-2 sm:p-4">
                    {loading && (
                        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 bg-slate-950/90 z-10">
                            <Loader2 size={36} className="animate-spin text-indigo-400" />
                            <p className="text-sm font-medium text-slate-400">
                                Loading resume preview...
                            </p>
                        </div>
                    )}

                    {error && (
                        <div className="flex h-full flex-col items-center justify-center gap-4 text-center p-6">
                            <div className="flex h-14 w-14 items-center justify-center rounded-2xl border border-rose-500/30 bg-rose-500/10 text-rose-400">
                                <AlertCircle size={28} />
                            </div>
                            <div>
                                <h4 className="font-bold text-white">Preview Unavailable</h4>
                                <p className="mt-1 text-sm text-slate-400 max-w-md">{error}</p>
                            </div>
                            <button
                                type="button"
                                onClick={() => setSelectedResumeId((id) => id)}
                                className="flex items-center gap-2 rounded-xl bg-slate-800 px-4 py-2 text-xs font-semibold text-slate-300 hover:bg-slate-700 hover:text-white"
                            >
                                <RefreshCw size={14} />
                                Retry
                            </button>
                        </div>
                    )}

                    {!loading && !error && pdfUrl && (
                        <iframe
                            src={`${pdfUrl}#toolbar=1&navpanes=0`}
                            title={activeResume?.originalFileName || 'Resume PDF'}
                            className="h-full w-full rounded-xl border border-slate-800 bg-slate-900"
                        />
                    )}
                </div>
            </div>
        </div>
    );
};
