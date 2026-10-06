import React, { useState } from 'react';
import { Lock, Mail, Loader2, LogIn, CheckCircle2, Eye, EyeOff } from 'lucide-react';
import { apiRequest, saveAuth } from '../services/api';
import type { ApiResponse, AuthResponse, Candidate } from '../types/api';
import { ForgotPasswordModal } from './ForgotPasswordModal';

interface LoginProps {
    onSuccess: (candidate: Candidate) => void;
    onRegister: () => void;
}

export const Login: React.FC<LoginProps> = ({
    onSuccess,
    onRegister,
}) => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [infoMessage, setInfoMessage] = useState<string | null>(null);
    const [isForgotPasswordOpen, setIsForgotPasswordOpen] = useState(false);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();

        setLoading(true);
        setError(null);
        setInfoMessage(null);

        try {
            const response =
                await apiRequest<ApiResponse<AuthResponse>>(
                    '/api/auth/login',
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            email,
                            password,
                        }),
                    }
                );

            if (!response.success || !response.data) {
                throw new Error(
                    response.message || 'Login failed.'
                );
            }

            saveAuth(response.data.token);
            onSuccess(response.data.candidate);
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to login.'
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4">
            <form
                onSubmit={submit}
                className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900 p-8 shadow-2xl"
            >
                <div className="mb-8 text-center">
                    <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600/20">
                        <LogIn
                            size={28}
                            className="text-indigo-400"
                        />
                    </div>

                    <h1 className="text-2xl font-bold text-white">
                        Welcome back
                    </h1>

                    <p className="mt-2 text-sm text-slate-400">
                        Sign in with your email and password.
                    </p>
                </div>

                {infoMessage && (
                    <div className="mb-5 flex items-center gap-2 rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3 text-sm text-emerald-300">
                        <CheckCircle2 size={16} className="shrink-0" />
                        <span>{infoMessage}</span>
                    </div>
                )}

                {error && (
                    <div className="mb-5 rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300">
                        {error}
                    </div>
                )}

                <label className="block text-sm font-medium text-slate-300">
                    Email
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
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                        placeholder="you@example.com"
                    />
                </div>

                <div className="mt-5 flex items-center justify-between">
                    <label className="block text-sm font-medium text-slate-300">
                        Password
                    </label>
                    <button
                        type="button"
                        onClick={() => setIsForgotPasswordOpen(true)}
                        className="text-xs font-medium text-indigo-400 hover:text-indigo-300 transition"
                    >
                        Forgot password?
                    </button>
                </div>

                <div className="relative mt-2">
                    <Lock
                        size={18}
                        className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                    />

                    <input
                        type={showPassword ? 'text' : 'password'}
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-11 text-sm text-white outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                        placeholder="Your password"
                    />

                    <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white transition p-1"
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                    >
                        {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </button>
                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="mt-7 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 disabled:opacity-50 transition"
                >
                    {loading ? (
                        <>
                            <Loader2
                                size={18}
                                className="animate-spin"
                            />
                            Signing in...
                        </>
                    ) : (
                        <>
                            <LogIn size={18} />
                            Sign In
                        </>
                    )}
                </button>

                <p className="mt-6 text-center text-sm text-slate-400">
                    Don't have an account?{' '}
                    <button
                        type="button"
                        onClick={onRegister}
                        className="font-semibold text-indigo-400 hover:text-indigo-300 transition"
                    >
                        Create one
                    </button>
                </p>
            </form>

            <ForgotPasswordModal
                isOpen={isForgotPasswordOpen}
                onClose={() => setIsForgotPasswordOpen(false)}
                initialEmail={email}
                onSuccess={(resetEmail) => {
                    setEmail(resetEmail);
                    setPassword('');
                    setInfoMessage('Password has been reset! Please sign in with your new password.');
                    setError(null);
                }}
            />
        </div>
    );
};