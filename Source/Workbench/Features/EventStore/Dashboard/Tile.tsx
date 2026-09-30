// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Tone, WidgetShell } from './WidgetShell';

export interface ITile {

    /**
     * The tile title.
     */
    title: string;

    /**
     * The icon shown before the title.
     */
    icon: ReactNode;

    /**
     * The tone of the tile.
     */
    tone: Tone;

    /**
     * The headline figure, or undefined while it is loading.
     */
    value: ReactNode | undefined;

    /**
     * What the headline figure counts.
     */
    caption: string;

    /**
     * Optional breakdown shown below the headline.
     */
    children?: ReactNode;

    /**
     * Optional link to where the figure can be acted on.
     */
    link?: { to: string, label: string };
}

/**
 * A metric tile: one headline figure, what it counts, an optional breakdown and a way to act on it.
 */
export const Tile = ({ title, icon, tone, value, caption, children, link }: ITile) => (
    <WidgetShell title={title} icon={icon} tone={tone}>
        <div className='dashboard-tile__hero'>
            <span className='dashboard-tile__value'>{value ?? '–'}</span>
            <span className='dashboard-tile__caption'>{caption}</span>
        </div>
        {children && <div className='dashboard-tile__details'>{children}</div>}
        {link && <Link className='dashboard-tile__link' to={link.to}>{link.label} →</Link>}
    </WidgetShell>
);

export interface IStat {

    /**
     * The tone of the dot in front of the figure.
     */
    tone: Tone;

    /**
     * The figure.
     */
    value: number;

    /**
     * What the figure counts.
     */
    label: string;
}

/**
 * One figure in a tile breakdown. A zero is dimmed, so the eye lands on what is actually present.
 */
export const Stat = ({ tone, value, label }: IStat) => (
    <span className={`dashboard-stat ${value === 0 ? 'dashboard-stat--zero' : ''}`}>
        <span className={`dashboard-dot dashboard-dot--${tone}`} aria-hidden='true' />
        <span className='dashboard-stat__value'>{value.toLocaleString()}</span>
        <span>{label}</span>
    </span>
);
