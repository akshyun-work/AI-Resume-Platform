import React, { useEffect, useState } from 'react';
import {
    Bot,
    Send,
    Loader2,
    User,
    AlertCircle,
    Eye,
    FileText,
} from 'lucide-react';

import { apiRequest } from '../services/api';

interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string | null;
}

interface ChatSession {
    id: string;
    candidateId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    messageCount: number;
}

interface ChatMessage {
    id: string;
    chatSessionId: string;
    sender: string;
    content: string;
    createdAt: string;
}

interface ResumeChatResponse {
    answer: string;
    conversation: ChatMessage[];
}

interface ResumeChatProps {
    resumeId?: string;
    onBack: () => void;
    onViewResume?: () => void;
}

export const ResumeChat: React.FC<ResumeChatProps> = ({
    resumeId,
    onBack,
    onViewResume,
}) => {
    const [session, setSession] =
        useState<ChatSession | null>(null);

    const [messages, setMessages] =
        useState<ChatMessage[]>([]);

    const [message, setMessage] =
        useState('');

    const [loading, setLoading] =
        useState(false);

    const [initializing, setInitializing] =
        useState(true);

    const [error, setError] =
        useState<string | null>(null);

    useEffect(() => {
        const initializeChat = async () => {
            if (!resumeId) {
                setInitializing(false);
                return;
            }

            try {
                setInitializing(true);
                setError(null);

                const sessionsResponse =
                    await apiRequest<
                        ApiResponse<ChatSession[]>
                    >('/api/chat/sessions');

                if (
                    !sessionsResponse.success ||
                    !sessionsResponse.data
                ) {
                    throw new Error(
                        sessionsResponse.message ||
                        'Unable to load chat sessions.'
                    );
                }

                let activeSession =
                    sessionsResponse.data[0];

                if (!activeSession) {
                    const createResponse =
                        await apiRequest<
                            ApiResponse<ChatSession>
                        >('/api/chat/sessions', {
                            method: 'POST',
                            headers: {
                                'Content-Type':
                                    'application/json',
                            },
                            body: JSON.stringify({
                                title: 'Resume Chat',
                            }),
                        });

                    if (
                        !createResponse.success ||
                        !createResponse.data
                    ) {
                        throw new Error(
                            createResponse.message ||
                            'Unable to create chat session.'
                        );
                    }

                    activeSession =
                        createResponse.data;
                }

                setSession(activeSession);

                const detailResponse =
                    await apiRequest<
                        ApiResponse<{
                            id: string;
                            candidateId: string;
                            title: string;
                            createdAt: string;
                            updatedAt: string;
                            messages: ChatMessage[];
                        }>
                    >(
                        `/api/chat/sessions/${activeSession.id}`
                    );

                if (
                    detailResponse.success &&
                    detailResponse.data
                ) {
                    setMessages(
                        detailResponse.data.messages || []
                    );
                }
            } catch (err) {
                console.error(err);

                setError(
                    err instanceof Error
                        ? err.message
                        : 'Unable to initialize chat.'
                );
            } finally {
                setInitializing(false);
            }
        };

        initializeChat();
    }, [resumeId]);

    const sendMessage = async (
        event: React.FormEvent
    ) => {
        event.preventDefault();

        if (
            !resumeId ||
            !session ||
            !message.trim() ||
            loading
        ) {
            return;
        }

        const currentMessage = message.trim();

        setMessage('');
        setLoading(true);
        setError(null);

        try {
            const response =
                await apiRequest<
                    ApiResponse<ResumeChatResponse>
                >(
                    `/api/chat/resumes/${resumeId}`,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/json',
                        },
                        body: JSON.stringify({
                            chatSessionId:
                                session.id,
                            message:
                                currentMessage,
                        }),
                    }
                );

            if (
                !response.success ||
                !response.data
            ) {
                throw new Error(
                    response.message ||
                    'Unable to generate chat response.'
                );
            }

            setMessages(
                response.data.conversation || []
            );
        } catch (err) {
            console.error(err);

            setError(
                err instanceof Error
                    ? err.message
                    : 'Unable to generate chat response.'
            );

            setMessage(currentMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="space-y-6">

            <div>
                <button
                    onClick={onBack}
                    className="mb-3 text-sm text-indigo-400 hover:text-indigo-300"
                >
                    ← Back to Dashboard
                </button>

                <h2 className="text-3xl font-bold text-white">
                    Resume AI Chat
                </h2>

                <p className="mt-2 text-slate-400">
                    Ask questions about your uploaded resume.
                </p>
            </div>

            {!resumeId ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-900 p-12 text-center">
                    <Bot
                        size={42}
                        className="mx-auto mb-4 text-slate-600"
                    />

                    <h3 className="text-lg font-bold text-white">
                        Upload a resume first
                    </h3>

                    <p className="mt-2 text-sm text-slate-400">
                        Resume Chat requires an uploaded resume.
                    </p>
                </div>
            ) : (
                <div className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">

                    <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-800 p-5">
                        <div className="flex items-center gap-3">
                            <div className="rounded-xl bg-indigo-500/10 p-2">
                                <Bot
                                    size={22}
                                    className="text-indigo-400"
                                />
                            </div>

                            <div>
                                <h3 className="font-bold text-white">
                                    Resume Assistant
                                </h3>

                                <p className="text-xs text-slate-500">
                                    {session?.title ||
                                        'Resume Chat'}
                                </p>
                            </div>
                        </div>

                        {onViewResume && (
                            <button
                                type="button"
                                onClick={onViewResume}
                                className="flex items-center gap-1.5 rounded-xl border border-indigo-500/30 bg-indigo-600/15 px-3.5 py-2 text-xs font-semibold text-indigo-300 transition hover:bg-indigo-600 hover:text-white"
                            >
                                <Eye size={14} />
                                View Resume PDF
                            </button>
                        )}
                    </div>

                    <div className="min-h-[450px] max-h-[550px] space-y-5 overflow-y-auto p-5">

                        {initializing ? (
                            <div className="flex items-center justify-center py-20">
                                <Loader2
                                    size={30}
                                    className="animate-spin text-indigo-400"
                                />
                            </div>
                        ) : messages.length === 0 ? (
                            <div className="py-20 text-center">
                                <Bot
                                    size={42}
                                    className="mx-auto mb-4 text-slate-600"
                                />

                                <h3 className="font-bold text-white">
                                    Ask me about your resume
                                </h3>

                                <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                                    Try asking about your skills,
                                    projects, experience, strengths,
                                    or areas for improvement.
                                </p>
                            </div>
                        ) : (
                            messages.map((item) => {
                                const isUser =
                                    item.sender.toLowerCase() ===
                                    'user';

                                return (
                                    <div
                                        key={item.id}
                                        className={`flex gap-3 ${isUser
                                                ? 'justify-end'
                                                : 'justify-start'
                                            }`}
                                    >
                                        {!isUser && (
                                            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-indigo-500/10">
                                                <Bot
                                                    size={18}
                                                    className="text-indigo-400"
                                                />
                                            </div>
                                        )}

                                        <div
                                            className={`max-w-[80%] rounded-2xl px-4 py-3 text-sm leading-6 ${isUser
                                                    ? 'bg-indigo-600 text-white'
                                                    : 'bg-slate-800 text-slate-200'
                                                }`}
                                        >
                                            {item.content}
                                        </div>

                                        {isUser && (
                                            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-800">
                                                <User
                                                    size={18}
                                                    className="text-slate-400"
                                                />
                                            </div>
                                        )}
                                    </div>
                                );
                            })
                        )}

                        {loading && (
                            <div className="flex items-center gap-3">
                                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-indigo-500/10">
                                    <Bot
                                        size={18}
                                        className="text-indigo-400"
                                    />
                                </div>

                                <div className="rounded-2xl bg-slate-800 px-4 py-3">
                                    <Loader2
                                        size={18}
                                        className="animate-spin text-indigo-400"
                                    />
                                </div>
                            </div>
                        )}
                    </div>

                    {error && (
                        <div className="mx-5 mb-4 flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                            <AlertCircle
                                size={18}
                                className="shrink-0"
                            />
                            <span>{error}</span>
                        </div>
                    )}

                    <form
                        onSubmit={sendMessage}
                        className="border-t border-slate-800 p-4"
                    >
                        <div className="flex gap-3">
                            <input
                                value={message}
                                onChange={(e) =>
                                    setMessage(
                                        e.target.value
                                    )
                                }
                                disabled={
                                    initializing ||
                                    loading ||
                                    !session
                                }
                                placeholder="Ask something about your resume..."
                                className="min-w-0 flex-1 rounded-xl border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-white outline-none focus:border-indigo-500 disabled:opacity-50"
                            />

                            <button
                                type="submit"
                                disabled={
                                    !message.trim() ||
                                    loading ||
                                    initializing ||
                                    !session
                                }
                                className="flex items-center justify-center gap-2 rounded-xl bg-indigo-600 px-5 py-3 text-sm font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-40"
                            >
                                {loading ? (
                                    <Loader2
                                        size={18}
                                        className="animate-spin"
                                    />
                                ) : (
                                    <Send size={18} />
                                )}

                                <span className="hidden sm:inline">
                                    Send
                                </span>
                            </button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
};