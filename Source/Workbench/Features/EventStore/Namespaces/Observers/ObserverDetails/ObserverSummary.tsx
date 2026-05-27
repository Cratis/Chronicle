// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { ObserverInformation, ObserverOwner, ObserverType } from 'Api/Observation';
import { getObserverRunningStateAsText } from '../getObserverRunningStateAsText';
import strings from 'Strings';
import './ObserverSummary.css';

/**
 * Props for {@link ObserverSummary}.
 */
export interface ObserverSummaryProps {
    /**
     * The observer to summarize.
     */
    observer: ObserverInformation;
}

const renderObserverType = (observer: ObserverInformation): string => {
    switch (observer.type) {
        case ObserverType.reactor:
            return strings.eventStore.namespaces.observers.types.reactor;
        case ObserverType.projection:
            return strings.eventStore.namespaces.observers.types.projection;
        case ObserverType.reducer:
            return strings.eventStore.namespaces.observers.types.reducer;
        case ObserverType.external:
            return strings.eventStore.namespaces.observers.types.external;
    }
    return strings.eventStore.namespaces.observers.types.unknown;
};

const renderObserverOwner = (observer: ObserverInformation): string => {
    switch (observer.owner) {
        case ObserverOwner.none:
            return strings.eventStore.namespaces.observers.owners.none;
        case ObserverOwner.client:
            return strings.eventStore.namespaces.observers.owners.client;
        case ObserverOwner.kernel:
            return strings.eventStore.namespaces.observers.owners.kernel;
    }
    return strings.eventStore.namespaces.observers.owners.none;
};

const renderBoolean = (value: boolean): string => (value ? 'Yes' : 'No');

/**
 * Renders a flat key/value summary of the observer.
 *
 * @param props - The {@link ObserverSummaryProps}.
 */
export const ObserverSummary = ({ observer }: ObserverSummaryProps) => {
    const summaryStrings = strings.eventStore.namespaces.observers.details.summary;

    const summaryRows: Array<{ label: string; value: string | number }> = [
        { label: summaryStrings.id, value: observer.id },
        { label: summaryStrings.type, value: renderObserverType(observer) },
        { label: summaryStrings.owner, value: renderObserverOwner(observer) },
        { label: summaryStrings.state, value: getObserverRunningStateAsText(observer.runningState) },
        { label: summaryStrings.sequence, value: observer.eventSequenceId },
        { label: summaryStrings.nextEventSequenceNumber, value: observer.nextEventSequenceNumber },
        { label: summaryStrings.lastHandledEventSequenceNumber, value: observer.lastHandledEventSequenceNumber },
        { label: summaryStrings.tailEventSequenceNumber, value: observer.tailEventSequenceNumber },
        { label: summaryStrings.isSubscribed, value: renderBoolean(observer.isSubscribed) },
        { label: summaryStrings.isReplayable, value: renderBoolean(observer.isReplayable) }
    ];

    return (
        <div className='observer-summary'>
            <DataTable
                value={summaryRows}
                dataKey='label'
                showHeaders={false}
                scrollable
                style={{ height: 'auto' }}>
                <Column field='label' style={{ width: '40%', color: 'var(--text-color-secondary)' }} />
                <Column field='value' />
            </DataTable>
        </div>
    );
};
