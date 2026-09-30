// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Card } from 'Components/Card';
import { ReactNode } from 'react';
import './Dashboard.css';

/**
 * The tones a widget can take - the color its frame and title carry, so the state of a figure reads before the
 * figure itself does.
 */
export type Tone = 'neutral' | 'success' | 'warning' | 'danger' | 'info';

export interface IWidgetShell {

    /**
     * The widget title.
     */
    title: string;

    /**
     * Optional icon shown before the title.
     */
    icon?: ReactNode;

    /**
     * Optional subtitle, describing what the widget is showing.
     */
    subtitle?: string;

    /**
     * Optional action rendered in the widget header.
     */
    action?: ReactNode;

    /**
     * The tone of the widget.
     */
    tone?: Tone;

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
export const WidgetShell = ({ title, icon, subtitle, action, tone = 'neutral', children, className }: IWidgetShell) => (
    <Card
        className={`dashboard-widget dashboard-widget--${tone} ${className ?? ''}`}
        header={
            <div className='dashboard-widget__header'>
                <div>
                    <span className='dashboard-widget__title'>{icon}{title}</span>
                    {subtitle && <span className='dashboard-widget__subtitle'>{subtitle}</span>}
                </div>
                {action}
            </div>
        }>
        {children}
    </Card>
);
