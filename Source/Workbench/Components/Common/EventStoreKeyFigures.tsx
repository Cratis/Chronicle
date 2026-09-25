// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ImDatabase, ImFolder, ImPriceTags } from 'react-icons/im';
import { StatisticsForEventStore } from 'Features/Statistics';
import strings from 'Strings';
import './EventStoreKeyFigures.css';

export interface IEventStoreKeyFigures {

    /**
     * The event store to show key figures for.
     */
    eventStore: string;
}

/**
 * Shows the key figures for an event store beneath its title on the landing page.
 */
export const EventStoreKeyFigures = ({ eventStore }: IEventStoreKeyFigures) => {
    const [result] = StatisticsForEventStore.use({ eventStore });

    // A store that has never been appended to has no statistics rows, which is not a failure - it reads as zero
    // across the board, which is exactly what it holds. Only an actually failed query says nothing.
    if (!result.isSuccess) {
        return (
            <div className='workbench-event-store-key-figures workbench-event-store-key-figures--unavailable'>
                {strings.home.keyFigures.unavailable}
            </div>
        );
    }

    const figures = [
        { icon: <ImDatabase aria-hidden='true' />, label: strings.home.keyFigures.events, value: result.data.totalEvents },
        { icon: <ImPriceTags aria-hidden='true' />, label: strings.home.keyFigures.eventTypes, value: result.data.eventTypes },
        { icon: <ImFolder aria-hidden='true' />, label: strings.home.keyFigures.namespaces, value: result.data.namespaces }
    ];

    return (
        <div className='workbench-event-store-key-figures'>
            {figures.map(figure => (
                <span key={figure.label} className='workbench-event-store-key-figures__figure' title={figure.label}>
                    {figure.icon}
                    <span className='workbench-event-store-key-figures__value'>{formatFigure(figure.value)}</span>
                    <span className='workbench-event-store-key-figures__label'>{figure.label}</span>
                </span>
            ))}
        </div>
    );
};

/**
 * Formats a figure so a busy store stays readable in the space a card footer has.
 * @param value The figure to format.
 * @returns The formatted figure.
 */
const formatFigure = (value: number | bigint): string => {
    // Event counts arrive as int64 and reach here as bigint, which Intl handles but arithmetic against a number
    // does not - so the thresholds are compared after a single widening rather than by mixing the two.
    const asNumber = typeof value === 'bigint' ? Number(value) : value;

    if (asNumber >= 1_000_000) return `${(asNumber / 1_000_000).toFixed(1)}M`;
    if (asNumber >= 1_000) return `${(asNumber / 1_000).toFixed(1)}k`;

    return asNumber.toLocaleString();
};
