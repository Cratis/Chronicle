// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventStatistics } from 'Features/Statistics';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IKeyFiguresWidget {

    /**
     * The statistics to show.
     */
    statistics: EventStatistics;

    /**
     * Optional subtitle, naming what the figures are scoped to.
     */
    subtitle?: string;

    /**
     * Additional class names.
     */
    className?: string;
}

/**
 * Shows the headline event figures.
 */
export const KeyFiguresWidget = ({ statistics, subtitle, className }: IKeyFiguresWidget) => {
    const figures = [
        { label: strings.eventStore.dashboard.totalEvents, value: statistics.totalEvents },
        { label: strings.eventStore.dashboard.eventTypes, value: statistics.eventTypes },
        { label: strings.eventStore.dashboard.namespaces, value: statistics.namespaces }
    ];

    return (
        <WidgetShell title={strings.eventStore.dashboard.keyFigures} subtitle={subtitle} className={className}>
            <div className='grid grid-cols-3 gap-4'>
                {figures.map(figure => (
                    <div key={figure.label} className='flex flex-col'>
                        <span className='text-3xl font-semibold'>{Number(figure.value).toLocaleString()}</span>
                        <span className='text-xs uppercase tracking-wide text-gray-400'>{figure.label}</span>
                    </div>
                ))}
            </div>
        </WidgetShell>
    );
};
