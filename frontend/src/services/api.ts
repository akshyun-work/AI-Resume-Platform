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
        throw new Error(
            data?.message ||
            data?.Message ||
            `Request failed with status ${response.status}`
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