import React, { useState, useEffect } from 'react';
import {
    X,
    Mail,
    Lock,
    KeyRound,
    Eye,
    EyeOff,
    CheckCircle2,
    ArrowLeft,
    RotateCcw,
    Loader2,
    ShieldCheck
} from 'lucide-react';
import { apiRequest } from '../services/api';
import type { ApiResponse } from '../types/api';

interface ForgotPasswordModalProps {
    isOpen: boolean;
    onClose: () => void;
    initialEmail?: string;
    onSuccess: (email: string) => void;
}

type Step = 'REQUEST_OTP' | 'VERIFY_AND_RESET' | 'SUCCESS';

export const ForgotPasswordModal: React.FC<ForgotPasswordModalProps> = ({
    isOpen,
    onClose,
    initialEmail = '',
    onSuccess,
}) => {
    const [step, setStep] = useState<Step>('REQUEST_OTP');
    const [email, setEmail] = useState('');
    const [otp, setOtp] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [resendCooldown, setResendCooldown] = useState(0);

    // Sync initial email when modal opens
    useEffect(() => {
        if (isOpen) {
            setEmail(initialEmail || '');
            setStep('REQUEST_OTP');
            setOtp('');
            setNewPassword('');
            setConfirmPassword('');
            setError(null);
            setSuccessMessage(null);
        }
    }, [isOpen, initialEmail]);

    // Cooldown countdown timer
    useEffect(() => {
        if (resendCooldown <= 0) return;
        const timer = setTimeout(() => {
            setResendCooldown((prev) => prev - 1);
        }, 1000);
        return () => clearTimeout(timer);
    }, [resendCooldown]);

    if (!isOpen) return null;

    // Step 1: Send OTP to email
    const handleSendOtp = async (e: React.FormEvent) => {
        e.preventDefault();
        const trimmedEmail = email.trim().toLowerCase();
        if (!trimmedEmail) {
            setError('Please enter your email address.');
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const response = await apiRequest<ApiResponse<boolean>>(
                '/api/auth/forgot-password',
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email: trimmedEmail }),
                }
            );

            if (!response.success) {
                throw new Error(response.message || 'Failed to send verification code.');
            }

            setSuccessMessage(response.message || 'Verification code sent to your email.');
            setStep('VERIFY_AND_RESET');
            setResendCooldown(60); // 60s cooldown for resend
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to send verification code.');
        } finally {
            setLoading(false);
        }
    };

    // Resend OTP
    const handleResendOtp = async () => {
        if (resendCooldown > 0 || loading) return;
        setLoading(true);
        setError(null);
        try {
            const response = await apiRequest<ApiResponse<boolean>>(
                '/api/auth/forgot-password',
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email: email.trim().toLowerCase() }),
                }
            );

            if (!response.success) {
                throw new Error(response.message || 'Failed to resend verification code.');
            }

            setSuccessMessage('A fresh verification code has been sent to your email.');
            setResendCooldown(60);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to resend code.');
        } finally {
            setLoading(false);
        }
    };

    // Step 2: Reset Password with OTP
    const handleResetPassword = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        const cleanOtp = otp.trim();
        if (cleanOtp.length !== 6) {
            setError('Please enter the 6-digit verification code.');
            return;
        }

        if (newPassword.length < 6) {
            setError('New password must be at least 6 characters.');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('Passwords do not match.');
            return;
        }

        setLoading(true);

        try {
            const response = await apiRequest<ApiResponse<boolean>>(
                '/api/auth/reset-password',
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        email: email.trim().toLowerCase(),
                        otp: cleanOtp,
                        newPassword,
                    }),
                }
            );

            if (!response.success) {
                throw new Error(response.message || 'Failed to reset password.');
            }

            setStep('SUCCESS');
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to reset password.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="relative w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900 p-8 shadow-2xl">
                {/* Close Button */}
                <button
                    onClick={onClose}
                    className="absolute right-5 top-5 rounded-full p-2 text-slate-400 hover:bg-slate-800 hover:text-white transition"
                    aria-label="Close"
                >
                    <X size={20} />
                </button>

                {/* Header Icon */}
                <div className="mb-6 text-center">
                    <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600/20 text-indigo-400">
                        {step === 'SUCCESS' ? (
                            <CheckCircle2 size={28} className="text-emerald-400" />
                        ) : step === 'VERIFY_AND_RESET' ? (
                            <ShieldCheck size={28} />
                        ) : (
                            <KeyRound size={28} />
                        )}
                    </div>

                    <h2 className="text-2xl font-bold text-white">
                        {step === 'SUCCESS'
                            ? 'Password Reset!'
                            : step === 'VERIFY_AND_RESET'
                            ? 'Reset Your Password'
                            : 'Forgot Password?'}
                    </h2>
                    <p className="mt-2 text-sm text-slate-400">
                        {step === 'SUCCESS'
                            ? 'Your password has been changed successfully.'
                            : step === 'VERIFY_AND_RESET'
                            ? `Enter the 6-digit code sent to ${email}`
                            : 'Enter your registered email address and we’ll send you a 6-digit OTP code to reset your password.'}
                    </p>
                </div>

                {/* Status Messages */}
                {error && (
                    <div className="mb-5 rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300">
                        {error}
                    </div>
                )}
                {successMessage && step === 'VERIFY_AND_RESET' && (
                    <div className="mb-5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3 text-sm text-emerald-300 flex items-center gap-2">
                        <CheckCircle2 size={16} className="shrink-0" />
                        <span>{successMessage}</span>
                    </div>
                )}

                {/* STEP 1: Request OTP */}
                {step === 'REQUEST_OTP' && (
                    <form onSubmit={handleSendOtp} className="space-y-5">
                        <div>
                            <label className="block text-sm font-medium text-slate-300">
                                Email Address
                            </label>
                            <div className="relative mt-2">
                                <Mail
                                    size={18}
                                    className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                                />
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    required
                                    autoFocus
                                    className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white placeholder:text-slate-500 outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                                    placeholder="you@example.com"
                                />
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={loading}
                            className="flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 transition disabled:opacity-50"
                        >
                            {loading ? (
                                <>
                                    <Loader2 size={18} className="animate-spin" />
                                    Sending Code...
                                </>
                            ) : (
                                'Send Verification Code'
                            )}
                        </button>

                        <button
                            type="button"
                            onClick={onClose}
                            className="flex w-full items-center justify-center gap-2 text-sm text-slate-400 hover:text-white transition pt-2"
                        >
                            <ArrowLeft size={16} />
                            Back to Sign In
                        </button>
                    </form>
                )}

                {/* STEP 2: Verify OTP & Reset Password */}
                {step === 'VERIFY_AND_RESET' && (
                    <form onSubmit={handleResetPassword} className="space-y-4">
                        {/* OTP Input */}
                        <div>
                            <div className="flex items-center justify-between">
                                <label className="block text-sm font-medium text-slate-300">
                                    6-Digit Verification Code (OTP)
                                </label>
                                <button
                                    type="button"
                                    onClick={() => {
                                        setStep('REQUEST_OTP');
                                        setError(null);
                                    }}
                                    className="text-xs text-indigo-400 hover:text-indigo-300"
                                >
                                    Change email
                                </button>
                            </div>
                            <div className="relative mt-2">
                                <KeyRound
                                    size={18}
                                    className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                                />
                                <input
                                    type="text"
                                    maxLength={6}
                                    value={otp}
                                    onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
                                    required
                                    autoFocus
                                    className="w-full tracking-widest text-center font-mono text-lg font-bold rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-white placeholder:text-slate-600 outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                                    placeholder="000000"
                                />
                            </div>
                        </div>

                        {/* New Password */}
                        <div>
                            <label className="block text-sm font-medium text-slate-300">
                                New Password
                            </label>
                            <div className="relative mt-2">
                                <Lock
                                    size={18}
                                    className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                                />
                                <input
                                    type={showPassword ? 'text' : 'password'}
                                    value={newPassword}
                                    onChange={(e) => setNewPassword(e.target.value)}
                                    required
                                    minLength={6}
                                    className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-10 text-sm text-white placeholder:text-slate-500 outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                                    placeholder="At least 6 characters"
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowPassword(!showPassword)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white"
                                >
                                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                                </button>
                            </div>
                        </div>

                        {/* Confirm New Password */}
                        <div>
                            <label className="block text-sm font-medium text-slate-300">
                                Confirm New Password
                            </label>
                            <div className="relative mt-2">
                                <Lock
                                    size={18}
                                    className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                                />
                                <input
                                    type={showPassword ? 'text' : 'password'}
                                    value={confirmPassword}
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    required
                                    minLength={6}
                                    className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white placeholder:text-slate-500 outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                                    placeholder="Re-enter your new password"
                                />
                            </div>
                        </div>

                        {/* Submit button */}
                        <button
                            type="submit"
                            disabled={loading || otp.length !== 6 || newPassword.length < 6}
                            className="mt-6 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 transition disabled:opacity-50"
                        >
                            {loading ? (
                                <>
                                    <Loader2 size={18} className="animate-spin" />
                                    Resetting Password...
                                </>
                            ) : (
                                'Reset Password'
                            )}
                        </button>

                        {/* Resend OTP */}
                        <div className="flex items-center justify-between pt-2">
                            <button
                                type="button"
                                onClick={() => setStep('REQUEST_OTP')}
                                className="flex items-center gap-1 text-xs text-slate-400 hover:text-white transition"
                            >
                                <ArrowLeft size={14} />
                                Change email
                            </button>

                            <button
                                type="button"
                                onClick={handleResendOtp}
                                disabled={resendCooldown > 0 || loading}
                                className="flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 disabled:text-slate-600 disabled:cursor-not-allowed transition"
                            >
                                <RotateCcw size={14} className={loading ? 'animate-spin' : ''} />
                                {resendCooldown > 0
                                    ? `Resend in ${resendCooldown}s`
                                    : 'Resend code'}
                            </button>
                        </div>
                    </form>
                )}

                {/* STEP 3: Success */}
                {step === 'SUCCESS' && (
                    <div className="space-y-6 text-center">
                        <div className="rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-4 text-sm text-emerald-300">
                            Your password has been reset successfully. You can now use your new password to sign in.
                        </div>

                        <button
                            type="button"
                            onClick={() => {
                                onSuccess(email);
                                onClose();
                            }}
                            className="flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 transition"
                        >
                            Sign In with New Password
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
};
