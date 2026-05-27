// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { TabView, TabPanel } from 'primereact/tabview';
import { ObserverInformation, ObserverOwner, ObserverType } from 'Api/Observation';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import strings from 'Strings';

/**
 * Props for {@link ObserverDetails}.
 */
export interface ObserverDetailsProps {
    /**
     * The observer to render details for.
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
 * Renders a summary panel and the list of event types consumed by the given observer.
 *
 * @param props - The {@link ObserverDetailsProps}.
 */
export const ObserverDetails = ({ observer }: ObserverDetailsProps) => {
    const summaryStrings = strings.eventStore.namespaces.observers.details.summary;
    const eventTypesStrings = strings.eventStore.namespaces.observers.details.eventTypes;
    const tabStrings = strings.eventStore.namespaces.observers.details.tabs;

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
        <div style={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0 }}>
            <TabView style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}
                panelContainerStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                <TabPanel header={tabStrings.summary}
                    contentStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                    <div style={{ flex: 1, minHeight: 0, padding: '8px 16px 16px 16px', overflow: 'auto' }}>
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
                </TabPanel>
                <TabPanel header={tabStrings.eventTypes}
                    contentStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                    <div style={{ flex: 1, minHeight: 0, padding: '0 16px 16px 16px' }}>
                        <DataTable
                            value={observer.eventTypes ?? []}
                            dataKey='id'
                            emptyMessage={eventTypesStrings.empty}
                            scrollable
                            scrollHeight='flex'
                            style={{ height: '100%' }}>
                            <Column
                                field='id'
                                header={eventTypesStrings.columns.id}
                                sortable />
                            <Column
                                field='generation'
                                header={eventTypesStrings.columns.generation}
                                sortable />
                        </DataTable>
                    </div>
                </TabPanel>
            </TabView>
        </div>
    );
};
