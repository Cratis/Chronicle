// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeCount } from 'Features/Statistics';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IEventTypeDistributionWidget {

    /**
     * The per event type counts, already ordered by the server.
     */
    perEventType: EventTypeCount[];

    /**
     * Additional class names.
     */
    className?: string;
}

/**
 * How many events of each type are held, as a share of the largest.
 */
export const EventTypeDistributionWidget = ({ perEventType, className }: IEventTypeDistributionWidget) => {
    if (perEventType.length === 0) {
        return (
            <WidgetShell title={strings.eventStore.dashboard.eventTypeDistribution} className={className}>
                <span className='text-sm text-gray-400'>{strings.eventStore.dashboard.noEvents}</span>
            </WidgetShell>
        );
    }

    // The server orders by count descending, so the first row is the largest and every bar is a share of it.
    // Scaling to the largest rather than to the total keeps the smaller types visible - against a total, a type
    // holding a fraction of a percent renders as nothing at all and reads as absent rather than small.
    const largest = Number(perEventType[0].count);

    return (
        <WidgetShell
            title={strings.eventStore.dashboard.eventTypeDistribution}
            subtitle={strings.eventStore.dashboard.eventTypeDistributionSubtitle}
            className={className}>
            <div className='flex flex-col gap-2 overflow-auto'>
                {perEventType.map(entry => (
                    <div key={entry.eventType} className='flex flex-col gap-1'>
                        <div className='flex justify-between gap-3 text-sm'>
                            <span className='truncate' title={entry.eventType}>{entry.eventType}</span>
                            <span className='shrink-0 font-semibold'>{Number(entry.count).toLocaleString()}</span>
                        </div>
                        <div className='h-1.5 w-full rounded bg-gray-700/50'>
                            <div
                                className='h-full rounded bg-[var(--cratis-primary-color)]'
                                style={{ width: `${largest === 0 ? 0 : (Number(entry.count) / largest) * 100}%` }}
                            />
                        </div>
                    </div>
                ))}
            </div>
        </WidgetShell>
    );
};
