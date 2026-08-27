import React, { useState } from 'react';
import { Lock, Mail, Loader2, LogIn } from 'lucide-react';
import { apiRequest, saveAuth } from '../services/api';
import type { ApiResponse, AuthResponse, Candidate } from '../types/api';

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
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();

        setLoading(true);
        setError(null);

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
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                        placeholder="you@example.com"
                    />
                </div>

                <label className="mt-5 block text-sm font-medium text-slate-300">
                    Password
                </label>

                <div className="relative mt-2">
                    <Lock
                        size={18}
                        className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                    />

                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                        placeholder="Your password"
                    />
                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="mt-7 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 disabled:opacity-50"
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
                        className="font-semibold text-indigo-400 hover:text-indigo-300"
                    >
                        Create one
                    </button>
                </p>
            </form>
        </div>
    );
};