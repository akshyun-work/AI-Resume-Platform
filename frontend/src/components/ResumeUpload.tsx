import React, { useRef, useState } from 'react';
import {
    FileUp,
    FileText,
    Loader2,
    CheckCircle2,
    AlertCircle,
} from 'lucide-react';

import { apiRequest } from '../services/api';
import type {
    ApiResponse,
    Resume,
    AtsAnalysis,
} from '../types/api';

interface ResumeUploadProps {
    onSuccess: (resume: Resume, analysis: AtsAnalysis) => void;
    onBack: () => void;
}

export const ResumeUpload: React.FC<ResumeUploadProps> = ({
    onSuccess,
    onBack,
}) => {
    const inputRef = useRef<HTMLInputElement>(null);

    const [file, setFile] = useState<File | null>(null);
    const [processing, setProcessing] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleFileChange = (
        event: React.ChangeEvent<HTMLInputElement>
    ) => {
        const selected = event.target.files?.[0];

        setError(null);

        if (!selected) {
            setFile(null);
            return;
        }

        if (
            selected.type !== 'application/pdf' &&
            !selected.name.toLowerCase().endsWith('.pdf')
        ) {
            setError('Only PDF files are allowed.');
            setFile(null);
            return;
        }

        if (selected.size > 10 * 1024 * 1024) {
            setError('Maximum file size is 10 MB.');
            setFile(null);
            return;
        }

        setFile(selected);
    };

    const uploadAndAnalyze = async () => {
        if (!file || processing) {
            return;
        }

        setProcessing(true);
        setError(null);

        try {
            // ---------------------------------------------
            // STEP 1: Upload resume
            // ---------------------------------------------

            const formData = new FormData();
            formData.append('File', file);

            const uploadResponse =
                await apiRequest<ApiResponse<Resume>>(
                    '/api/resumes',
                    {
                        method: 'POST',
                        body: formData,
                    }
                );

            if (
                !uploadResponse.success ||
                !uploadResponse.data
            ) {
                throw new Error(
                    uploadResponse.message ||
                    'Resume upload failed.'
                );
            }

            const resume = uploadResponse.data;

            // ---------------------------------------------
            // STEP 2: Run ATS analysis
            // ---------------------------------------------

            const analysisResponse =
                await apiRequest<ApiResponse<AtsAnalysis>>(
                    '/api/ats',
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            resumeId: resume.id,
                        }),
                    }
                );

            if (
                !analysisResponse.success ||
                !analysisResponse.data
            ) {
                throw new Error(
                    analysisResponse.message ||
                    'ATS analysis failed.'
                );
            }

            // ---------------------------------------------
            // STEP 3: Return results to App
            // ---------------------------------------------

            onSuccess(
                resume,
                analysisResponse.data
            );

            setFile(null);

            if (inputRef.current) {
                inputRef.current.value = '';
            }
        } catch (err) {
            console.error(err);

            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to upload and analyze resume.'
            );
        } finally {
            setProcessing(false);
        }
    };

    return (
        <div className="w-full max-w-2xl">

            {/* Back button */}
            <button
                type="button"
                onClick={onBack}
                disabled={processing}
                className="mb-5 flex items-center gap-2 text-sm font-medium text-indigo-400 transition hover:text-indigo-300 disabled:cursor-not-allowed disabled:opacity-50"
            >
                ← Back to Dashboard
            </button>

            {/* Upload card */}
            <div className="w-full rounded-3xl border border-slate-800 bg-slate-900/80 p-8 shadow-2xl backdrop-blur">

                {/* Header */}
                <div className="mb-8 text-center">
                    <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600/20">
                        <FileUp
                            size={30}
                            className="text-indigo-400"
                        />
                    </div>

                    <h2 className="text-2xl font-bold text-white">
                        Upload Your Resume
                    </h2>

                    <p className="mt-2 text-sm leading-6 text-slate-400">
                        Upload your PDF resume and the AI pipeline
                        will analyze it and generate your ATS score.
                    </p>
                </div>

                {/* Hidden file input */}
                <input
                    ref={inputRef}
                    type="file"
                    accept=".pdf,application/pdf"
                    onChange={handleFileChange}
                    className="hidden"
                />

                {/* File selector */}
                <button
                    type="button"
                    onClick={() => inputRef.current?.click()}
                    disabled={processing}
                    className="w-full rounded-2xl border-2 border-dashed border-slate-700 bg-slate-950/50 p-10 transition hover:border-indigo-500/60 hover:bg-indigo-500/5 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    <FileUp
                        size={40}
                        className="mx-auto mb-4 text-indigo-400"
                    />

                    <p className="font-semibold text-white">
                        Click to choose a PDF
                    </p>

                    <p className="mt-2 text-xs text-slate-500">
                        Maximum size: 10 MB
                    </p>
                </button>

                {/* Selected file */}
                {file && (
                    <div className="mt-5 flex items-center gap-4 rounded-xl border border-slate-700 bg-slate-950/60 p-4">
                        <FileText
                            size={28}
                            className="shrink-0 text-indigo-400"
                        />

                        <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-semibold text-white">
                                {file.name}
                            </p>

                            <p className="mt-1 text-xs text-slate-500">
                                {(file.size / (1024 * 1024)).toFixed(2)} MB
                            </p>
                        </div>

                        <CheckCircle2
                            size={20}
                            className="text-emerald-400"
                        />
                    </div>
                )}

                {/* Error */}
                {error && (
                    <div className="mt-5 flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                        <AlertCircle
                            size={18}
                            className="shrink-0"
                        />

                        <span>{error}</span>
                    </div>
                )}

                {/* Analyze button */}
                <button
                    type="button"
                    onClick={uploadAndAnalyze}
                    disabled={!file || processing}
                    className="mt-6 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-5 py-3 font-semibold text-white shadow-lg shadow-indigo-600/20 transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-40"
                >
                    {processing ? (
                        <>
                            <Loader2
                                size={18}
                                className="animate-spin"
                            />

                            Analyzing Resume...
                        </>
                    ) : (
                        <>
                            <FileUp size={18} />

                            Upload & Analyze Resume
                        </>
                    )}
                </button>

            </div>
        </div>
    );
};