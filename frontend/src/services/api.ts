const API_BASE_URL = 'http://localhost:5000';
const FACE_API_BASE_URL = 'http://localhost:5034';

export const apiRequest = async <T>(
    path: string,
    options: RequestInit = {}
): Promise<T> => {
    const token = localStorage.getItem('authToken');

    const headers = new Headers(options.headers);

    if (token) {
        headers.set(
            'Authorization',
            `Bearer ${token}`
        );
    }

    const response = await fetch(
        `${API_BASE_URL}${path}`,
        {
            ...options,
            headers,
        }
    );

    const text = await response.text();

    let data: any = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = text;
        }
    }

    if (!response.ok) {
        if (response.status === 401) {
            if (!path.includes('/api/auth/')) {
                clearAuth();
                window.dispatchEvent(new CustomEvent('auth-expired'));
                throw new Error('Your session has expired. Please log in again.');
            }
        }

        throw new Error(
            data?.message ||
            data?.Message ||
            (response.status === 401 ? 'Invalid email or password.' : `Request failed with status ${response.status}`)
        );
    }

    return data as T;
};

export const faceApiRequest = async <T>(
    path: string,
    body: FormData
): Promise<T> => {
    const token = localStorage.getItem('authToken');

    const headers = new Headers();

    if (token) {
        headers.set(
            'Authorization',
            `Bearer ${token}`
        );
    }

    const response = await fetch(
        `${FACE_API_BASE_URL}${path}`,
        {
            method: 'POST',
            headers,
            body,
        }
    );

    const text = await response.text();

    let data: any = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = text;
        }
    }

    if (!response.ok) {
        throw new Error(
            data?.message ||
            data?.Message ||
            `Face API request failed with status ${response.status}`
        );
    }

    return data as T;
};

export const saveAuth = (
    token: string
) => {
    localStorage.setItem(
        'authToken',
        token
    );
};

export const getAuthToken = () => {
    return localStorage.getItem(
        'authToken'
    );
};

export const clearAuth = () => {
    localStorage.removeItem(
        'authToken'
    );
};

export const fetchResumeBlob = async (resumeId: string): Promise<Blob> => {
    const token = localStorage.getItem('authToken');
    const headers = new Headers();
    if (token) {
        headers.set('Authorization', `Bearer ${token}`);
    }

    const response = await fetch(`${API_BASE_URL}/api/resumes/${resumeId}/view`, {
        headers,
    });

    if (!response.ok) {
        if (response.status === 401) {
            clearAuth();
            window.dispatchEvent(new CustomEvent('auth-expired'));
            throw new Error('Your session has expired. Please log in again.');
        }
        throw new Error(`Unable to load resume PDF (Status ${response.status})`);
    }

    return await response.blob();
};

export const downloadResumePdf = async (resumeId: string, fileName = 'resume.pdf'): Promise<void> => {
    const blob = await fetchResumeBlob(resumeId);
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};