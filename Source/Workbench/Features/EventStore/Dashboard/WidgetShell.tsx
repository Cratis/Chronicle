// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'Components/Card';
import { ReactNode } from 'react';

export interface IWidgetShell {

    /**
     * The widget title.
     */
    title: string;

    /**
     * Optional subtitle, describing what the widget is showing.
     */
    subtitle?: string;

    /**
     * Optional action rendered in the widget header.
     */
    action?: ReactNode;

    /**
     * The widget content.
     */
    children?: ReactNode;

    /**
     * Additional class names.
     */
    className?: string;
}

/**
 * The frame every dashboard widget sits in, so they line up and read as one surface.
 */
export const WidgetShell = ({ title, subtitle, action, children, className }: IWidgetShell) => (
    <Card
        className={`panel h-full rounded-xl border border-gray-700/60 shadow-sm ${className ?? ''}`}
        header={
            <div className='flex items-start justify-between gap-3'>
                <div className='flex flex-col'>
                    <span className='text-sm uppercase tracking-wide text-gray-300'>{title}</span>
                    {subtitle && <span className='text-xs text-gray-500'>{subtitle}</span>}
                </div>
                {action}
            </div>
        }>
        <div className='flex-1 min-h-0'>
            {children}
        </div>
    </Card>
);
