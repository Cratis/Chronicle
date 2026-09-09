// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, ReactNode, useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { absolutePath } from '../../Utils/basePath';
import { clearAntiforgeryToken, getAntiforgeryHeaders, refreshAntiforgeryToken } from './antiforgery';

export interface AuthContextType {
    isAuthenticated: boolean;
    isAuthenticationEnabled: boolean;
    isLoading: boolean;
    checkAuth: () => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) throw new Error('useAuth must be used within AuthProvider');
    return context;
};

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isAuthenticationEnabled, setIsAuthenticationEnabled] = useState(true);
    const navigate = useNavigate();

    const checkAuth = async () => {
        try {
            // This endpoint is protected with authentication on, and explicitly returns 204 with it off.
            setIsAuthenticationEnabled(await refreshAntiforgeryToken());
            setIsAuthenticated(true);
        } catch {
            clearAntiforgeryToken();
            setIsAuthenticated(false);
            navigate(absolutePath('/login'));
        } finally {
            setIsLoading(false);
        }
    };

    const logout = async () => {
        if (!isAuthenticationEnabled) return;
        // Do not pretend logout succeeded when the server still has an active session.
        const response = await fetch(absolutePath('/identity/logout'), {
            method: 'POST',
            credentials: 'include',
            headers: getAntiforgeryHeaders(),
        });
        if (!response.ok) throw new Error('Logout failed. Please try again.');
        clearAntiforgeryToken();
        setIsAuthenticated(false);
        navigate(absolutePath('/login'));
    };

    useEffect(() => { checkAuth(); }, []);

    return <AuthContext.Provider value={{ isAuthenticated, isAuthenticationEnabled, isLoading, checkAuth, logout }}>{children}</AuthContext.Provider>;
};
