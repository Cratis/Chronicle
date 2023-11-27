// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, ReactElement } from 'react';

interface ErrorBoundaryProps {
    children: ReactElement;
}

interface ErrorState {
    hasError: boolean;
    error: Error;
}

interface ErrorBoundaryWrapperProps {
    children: ReactElement;
    onError: (error: Error) => void;
}

const ErrorBoundaryWrapper = ({ children, onError }: ErrorBoundaryWrapperProps) => {
    useEffect(() => {
        const errorHandler = (event: CustomEvent) => {
            onError(new Error(event.detail.message));
        };

        window.addEventListener('customErrorEvent', errorHandler as EventListener);

        return () => {
            window.removeEventListener('customErrorEvent', errorHandler as EventListener);
        };
    }, [onError]);

    return children;
};

export const ErrorBoundary = ({ children }: ErrorBoundaryProps) => {
    const [errorState, setErrorState] = useState<ErrorState>({
        hasError: false,
        error: new Error(),
    });

    const getDerivedStateFromError = (error: Error): ErrorState => {
        return { hasError: true, error: error };
    };
    useEffect(() => {
        if (errorState.hasError) {
            console.error('Uncaught error:', errorState.error);
        }
    }, [errorState]);

    const handleError = (error: Error) => {
        setErrorState(getDerivedStateFromError(error));
    };

    if (errorState.hasError) {
        return (
            <div className='p-4'>
                <h1 className='text-3xl m-3'>Error</h1>
                <p>{errorState.error.message}</p>
                <p>{errorState.error.stack}</p>
            </div>
        );
    }
    return <ErrorBoundaryWrapper onError={handleError}>{children}</ErrorBoundaryWrapper>;
};
