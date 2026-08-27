import React, { useEffect, useRef, useState } from 'react';
import {
    Camera,
    CheckCircle2,
    X,
    Loader2,
    AlertCircle,
} from 'lucide-react';

import {
    faceApiRequest,
    saveAuth,
} from '../services/api';

import type {
    Candidate,
    FaceLoginResponse,
} from '../types/api';

interface FaceAuthModalProps {
    mode: 'register' | 'login';
    candidateId?: string;

    onSuccess: (
        candidate?: Candidate
    ) => void;

    onClose: () => void;

    onError?: (message: string) => void;
}

export const FaceAuthModal: React.FC<FaceAuthModalProps> = ({
    mode,
    candidateId,
    onSuccess,
    onClose,
    onError,
}) => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    const streamRef = useRef<MediaStream | null>(null);

    const [cameraReady, setCameraReady] = useState(false);
    const [processing, setProcessing] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        startCamera();

        return () => {
            stopCamera();
        };
    }, []);

    const startCamera = async () => {
        try {
            setError(null);

            const stream =
                await navigator.mediaDevices.getUserMedia({
                    video: {
                        facingMode: 'user',
                        width: {
                            ideal: 640,
                        },
                        height: {
                            ideal: 640,
                        },
                    },
                    audio: false,
                });

            streamRef.current = stream;

            if (videoRef.current) {
                videoRef.current.srcObject = stream;

                await videoRef.current.play();
            }

            setCameraReady(true);
        } catch (err) {
            console.error(err);

            const message =
                'Unable to access the camera. Please check browser camera permissions.';

            setError(message);
            onError?.(message);
        }
    };

    const stopCamera = () => {
        if (streamRef.current) {
            streamRef.current
                .getTracks()
                .forEach((track) => track.stop());

            streamRef.current = null;
        }
    };

    const captureImage = async (): Promise<File | null> => {
        const video = videoRef.current;
        const canvas = canvasRef.current;

        if (!video || !canvas) {
            return null;
        }

        if (
            video.readyState <
            HTMLMediaElement.HAVE_CURRENT_DATA
        ) {
            return null;
        }

        const width = video.videoWidth;
        const height = video.videoHeight;

        if (!width || !height) {
            return null;
        }

        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext('2d');

        if (!context) {
            return null;
        }

        context.drawImage(
            video,
            0,
            0,
            width,
            height
        );

        return new Promise((resolve) => {
            canvas.toBlob(
                (blob) => {
                    if (!blob) {
                        resolve(null);
                        return;
                    }

                    const file = new File(
                        [blob],
                        'face-capture.jpg',
                        {
                            type: 'image/jpeg',
                        }
                    );

                    resolve(file);
                },
                'image/jpeg',
                0.92
            );
        });
    };

    const verifyOrRegister = async () => {
        if (processing) {
            return;
        }

        setError(null);

        if (!cameraReady) {
            const message = 'Camera is not ready yet.';

            setError(message);
            onError?.(message);

            return;
        }

        setProcessing(true);

        try {
            const image = await captureImage();

            if (!image) {
                throw new Error(
                    'Unable to capture an image from the camera.'
                );
            }

            const formData = new FormData();

            formData.append(
                'Image',
                image,
                'face-capture.jpg'
            );

            if (mode === 'register') {
                if (!candidateId) {
                    throw new Error(
                        'Candidate ID is required for face registration.'
                    );
                }

                formData.append(
                    'CandidateId',
                    candidateId
                );

                await faceApiRequest<{
                    message: string;
                }>(
                    '/api/FaceRecognition/register',
                    formData
                );

                stopCamera();

                onSuccess();
                return;
            }

            const response =
                await faceApiRequest<FaceLoginResponse>(
                    '/api/FaceRecognition/login',
                    formData
                );

            saveAuth(response.token);

            stopCamera();

            onSuccess(response.candidate);
        } catch (err) {
            console.error(err);

            const message =
                err instanceof Error
                    ? err.message
                    : 'Face authentication failed.';

            setError(message);
            onError?.(message);
        } finally {
            setProcessing(false);
        }
    };

    const closeModal = () => {
        stopCamera();
        onClose();
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
            <div className="w-full max-w-lg rounded-3xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
                {/* Header */}
                <div className="mb-5 flex items-center justify-between">
                    <div>
                        <h2 className="text-xl font-bold text-white">
                            {mode === 'register'
                                ? 'Register Face ID'
                                : 'Sign in with Face ID'}
                        </h2>

                        <p className="mt-1 text-xs text-slate-500">
                            {mode === 'register'
                                ? 'Capture your face to enable optional biometric authentication.'
                                : 'Position your face clearly inside the camera frame.'}
                        </p>
                    </div>

                    <button
                        type="button"
                        onClick={closeModal}
                        className="flex h-9 w-9 items-center justify-center rounded-xl text-slate-400 transition hover:bg-slate-800 hover:text-white"
                    >
                        <X size={19} />
                    </button>
                </div>

                {/* Camera */}
                <div className="relative overflow-hidden rounded-2xl border border-indigo-500/40 bg-black">
                    <video
                        ref={videoRef}
                        autoPlay
                        muted
                        playsInline
                        className="aspect-square w-full object-cover"
                    />

                    {!cameraReady && (
                        <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-950">
                            <Loader2
                                size={30}
                                className="animate-spin text-indigo-400"
                            />

                            <p className="mt-3 text-sm text-slate-400">
                                Starting camera...
                            </p>
                        </div>
                    )}

                    {/* Face guide */}
                    {cameraReady && (
                        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
                            <div className="h-[72%] w-[58%] rounded-[50%] border-2 border-indigo-400/80" />
                        </div>
                    )}
                </div>

                <canvas
                    ref={canvasRef}
                    className="hidden"
                />

                {/* Error */}
                {error && (
                    <div className="mt-4 flex items-start gap-3 rounded-xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
                        <AlertCircle
                            size={18}
                            className="mt-0.5 shrink-0"
                        />

                        <span>{error}</span>
                    </div>
                )}

                {/* Buttons */}
                <div className="mt-5 flex gap-3">
                    <button
                        type="button"
                        onClick={closeModal}
                        disabled={processing}
                        className="flex-1 rounded-xl border border-slate-700 px-4 py-3 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 disabled:opacity-50"
                    >
                        Cancel
                    </button>

                    <button
                        type="button"
                        onClick={verifyOrRegister}
                        disabled={!cameraReady || processing}
                        className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {processing ? (
                            <>
                                <Loader2
                                    size={17}
                                    className="animate-spin"
                                />

                                {mode === 'register'
                                    ? 'Registering...'
                                    : 'Verifying...'}
                            </>
                        ) : (
                            <>
                                {mode === 'register' ? (
                                    <Camera size={17} />
                                ) : (
                                    <CheckCircle2 size={17} />
                                )}

                                {mode === 'register'
                                    ? 'Register Face'
                                    : 'Verify Face'}
                            </>
                        )}
                    </button>
                </div>
            </div>
        </div>
    );
};