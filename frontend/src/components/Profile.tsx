import React, { useState } from 'react';
import {
    ArrowLeft,
    UserCircle,
    Mail,
    Phone,
    ShieldCheck,
    Camera,
    CheckCircle2,
    AlertCircle,
} from 'lucide-react';

import { FaceAuthModal } from './FaceAuthModal';
import type { Candidate } from '../types/api';

interface ProfileProps {
    candidate: Candidate;
    onBack: () => void;
}

export const Profile: React.FC<ProfileProps> = ({
    candidate,
    onBack,
}) => {
    const [showFaceModal, setShowFaceModal] = useState(false);

    const [faceRegistered, setFaceRegistered] = useState(false);
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const handleFaceRegistered = () => {
        setFaceRegistered(true);
        setMessage('Face ID registered successfully.');
        setError(null);
        setShowFaceModal(false);
    };

    const handleFaceError = (errorMessage: string) => {
        setError(errorMessage);
        setMessage(null);
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

                    {/* Candidate ID */}
                    <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-slate-500">
                            <ShieldCheck size={15} />
                            Candidate ID
                        </div>

                        <p className="mt-2 break-all font-mono text-xs text-slate-300">
                            {candidate.id}
                        </p>
                    </div>
                </div>
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
                            Optional biometric authentication
                        </p>

                        <div className="mt-3">
                            {faceRegistered ? (
                                <span className="flex w-fit items-center gap-2 text-xs font-medium text-emerald-400">
                                    <CheckCircle2 size={15} />
                                    Face ID registered
                                </span>
                            ) : (
                                <span className="text-xs text-slate-500">
                                    Not registered
                                </span>
                            )}
                        </div>
                    </div>

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
                            ? 'Register Again'
                            : 'Register Face ID'}
                    </button>
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
        </div>
    );
};