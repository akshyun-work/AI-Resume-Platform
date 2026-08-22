import React, { useRef, useState, useCallback } from 'react';
import { Camera, RefreshCw, CheckCircle2, AlertCircle } from 'lucide-react';

interface FaceAuthProps {
  mode: 'login' | 'register';
  userId?: number;
  onSuccess: (userId?: number) => void;
}

export const FaceAuthModal: React.FC<FaceAuthProps> = ({ mode, userId, onSuccess }) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const startCamera = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { width: 640, height: 640, facingMode: 'user' }
      });
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
      }
    } catch (err) {
      setError('Camera access denied. Please grant webcam permissions.');
    }
  };

  const captureAndSubmit = useCallback(async () => {
    if (!videoRef.current) return;
    setIsScanning(true);
    setError(null);

    const canvas = document.createElement('canvas');
    canvas.width = videoRef.current.videoWidth || 640;
    canvas.height = videoRef.current.videoHeight || 480;
    const ctx = canvas.getContext('2d');
    ctx?.drawImage(videoRef.current, 0, 0);

    canvas.toBlob(async (blob) => {
      if (!blob) return;

      const formData = new FormData();
      formData.append('Image', blob, 'face_capture.jpg');
      if (mode === 'register' && userId) {
        formData.append('UserId', userId.toString());
      }

      const endpoint = mode === 'login' 
        ? '/api/FaceRecognition/login' 
        : '/api/FaceRecognition/register';

      try {
        const response = await fetch(`https://localhost:7098${endpoint}`, {
          method: 'POST',
          body: formData,
        });

        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Face verification failed');

        onSuccess(data.userId);
      } catch (err: any) {
        setError(err.message || 'Unable to connect to Face API.');
      } finally {
        setIsScanning(false);
      }
    }, 'image/jpeg');
  }, [mode, userId, onSuccess]);

  return (
    <div className="flex flex-col items-center bg-gray-900 border border-slate-800 p-6 rounded-2xl max-w-md w-full shadow-2xl">
      <h3 className="text-xl font-bold text-white mb-2">
        {mode === 'login' ? 'Biometric Face Login' : 'Register Face ID'}
      </h3>
      <p className="text-xs text-slate-400 mb-6 text-center">
        Position your face inside the circle for AI embedding verification.
      </p>

      <div className="relative w-64 h-64 rounded-full overflow-hidden border-4 border-indigo-500/40 shadow-inner flex items-center justify-center bg-black">
        <video
          ref={videoRef}
          autoPlay
          playsInline
          muted
          onLoadedMetadata={() => videoRef.current?.play()}
          className="w-full h-full object-cover transform -scale-x-100"
        />

        {isScanning && (
          <div className="absolute inset-0 bg-gradient-to-b from-transparent via-indigo-500/30 to-transparent animate-pulse pointer-events-none" />
        )}
      </div>

      {error && (
        <div className="mt-4 flex items-center gap-2 text-rose-400 text-xs">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      <div className="mt-6 flex gap-3 w-full">
        <button
          onClick={startCamera}
          className="flex-1 py-2.5 rounded-xl border border-slate-700 hover:bg-slate-800 text-slate-300 font-medium text-xs flex items-center justify-center gap-2 transition"
        >
          <Camera size={16} /> Open Camera
        </button>
        <button
          onClick={captureAndSubmit}
          disabled={isScanning}
          className="flex-1 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white font-medium text-xs flex items-center justify-center gap-2 shadow-lg shadow-indigo-600/30 transition"
        >
          {isScanning ? <RefreshCw className="animate-spin" size={16} /> : <CheckCircle2 size={16} />}
          Verify Face
        </button>
      </div>
    </div>
  );
};
