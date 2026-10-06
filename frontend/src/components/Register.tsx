import React, { useState } from 'react';
import {
    User,
    Mail,
    Lock,
    Phone,
    UserPlus,
    Loader2,
    Eye,
    EyeOff,
} from 'lucide-react';

import { apiRequest, saveAuth } from '../services/api';
import type { ApiResponse, AuthResponse, Candidate } from '../types/api';

interface RegisterProps {
    onSuccess: (candidate: Candidate) => void;
    onLogin: () => void;
}

export const Register: React.FC<RegisterProps> = ({
    onSuccess,
    onLogin,
}) => {
    const [fullName, setFullName] = useState('');
    const [email, setEmail] = useState('');
    const [phone, setPhone] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();

        setLoading(true);
        setError(null);

        try {
            const response =
                await apiRequest<ApiResponse<AuthResponse>>(
                    '/api/auth/register',
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            fullName,
                            email,
                            phone: phone || null,
                            password,
                        }),
                    }
                );

            if (!response.success || !response.data) {
                throw new Error(
                    response.message || 'Registration failed.'
                );
            }

            saveAuth(response.data.token);
            onSuccess(response.data.candidate);
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to register.'
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10">
            <form
                onSubmit={submit}
                className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900 p-8 shadow-2xl"
            >
                <div className="mb-7 text-center">
                    <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600/20">
                        <UserPlus
                            size={28}
                            className="text-indigo-400"
                        />
                    </div>

                    <h1 className="text-2xl font-bold text-white">
                        Create your account
                    </h1>

                    <p className="mt-2 text-sm text-slate-400">
                        Email and password are your primary login method.
                    </p>
                </div>

                {error && (
                    <div className="mb-5 rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300">
                        {error}
                    </div>
                )}

                <div className="space-y-4">
                    <div>
                        <label className="text-sm text-slate-300">
                            Full name
                        </label>

                        <div className="relative mt-2">
                            <User
                                size={17}
                                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                            />

                            <input
                                required
                                value={fullName}
                                onChange={(e) =>
                                    setFullName(e.target.value)
                                }
                                className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="text-sm text-slate-300">
                            Email
                        </label>

                        <div className="relative mt-2">
                            <Mail
                                size={17}
                                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                            />

                            <input
                                required
                                type="email"
                                value={email}
                                onChange={(e) =>
                                    setEmail(e.target.value)
                                }
                                className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="text-sm text-slate-300">
                            Phone
                        </label>

                        <div className="relative mt-2">
                            <Phone
                                size={17}
                                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                            />

                            <input
                                value={phone}
                                onChange={(e) =>
                                    setPhone(e.target.value)
                                }
                                className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-4 text-sm text-white outline-none focus:border-indigo-500"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="text-sm text-slate-300">
                            Password
                        </label>

                        <div className="relative mt-2">
                            <Lock
                                size={17}
                                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                            />

                            <input
                                required
                                minLength={8}
                                type={showPassword ? 'text' : 'password'}
                                value={password}
                                onChange={(e) =>
                                    setPassword(e.target.value)
                                }
                                className="w-full rounded-xl border border-slate-700 bg-slate-950 py-3 pl-10 pr-11 text-sm text-white outline-none focus:border-indigo-500"
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
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="mt-6 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-500 disabled:opacity-50"
                >
                    {loading ? (
                        <>
                            <Loader2
                                size={18}
                                className="animate-spin"
                            />
                            Creating account...
                        </>
                    ) : (
                        <>
                            <UserPlus size={18} />
                            Create Account
                        </>
                    )}
                </button>

                <p className="mt-5 text-center text-sm text-slate-400">
                    Already have an account?{' '}
                    <button
                        type="button"
                        onClick={onLogin}
                        className="font-semibold text-indigo-400"
                    >
                        Sign in
                    </button>
                </p>
            </form>
        </div>
    );
};