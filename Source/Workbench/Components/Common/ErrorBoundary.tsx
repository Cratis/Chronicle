// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, ErrorInfo, ReactNode } from 'react';

import { tw } from 'typewind';

interface Props {
    children: ReactNode;
}
interface State {
    hasError: boolean;
    error: Error;
}

export class ErrorBoundary extends Component<Props, State> {
    public state: State = {
        hasError: false,
        error: new Error(),
    };

    public static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error: error };
    }

    public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
        console.error('Uncaught error:', error, errorInfo);
    }

    public render() {
        if (this.state.hasError) {
            return (
                <div className={tw.p_4}>
                    <h1 className={tw.text_3xl.m_3}>Error</h1>
                    <p>{this.state.error.message}</p>
                    <p>{this.state.error.stack}</p>
                </div>
            );
        }

        return this.props.children;
    }
}
