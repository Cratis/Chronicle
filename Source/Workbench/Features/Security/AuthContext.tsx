// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, ReactNode, useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export interface AuthContextType {
    isAuthenticated: boolean;
    isLoading: boolean;
    checkAuth: () => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider');
    }
    return context;
};

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    const checkAuth = async () => {
        try {
            // Try to access a protected endpoint to verify authentication
            const response = await fetch('/api/event-stores', {
                credentials: 'include',
            });

            if (response.ok) {
                setIsAuthenticated(true);
            } else if (response.status === 401) {
                setIsAuthenticated(false);
                navigate('/login');
            }
        } catch (error) {
            console.error('Auth check error:', error);
            setIsAuthenticated(false);
        } finally {
            setIsLoading(false);
        }
    };

    const logout = async () => {
        try {
            await fetch('/identity/logout', {
                method: 'POST',
                credentials: 'include',
            });
            setIsAuthenticated(false);
            navigate('/login');
        } catch (error) {
            console.error('Logout error:', error);
        }
    };

    useEffect(() => {
        checkAuth();
    }, []);

    const value: AuthContextType = {
        isAuthenticated,
        isLoading,
        checkAuth,
        logout,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
