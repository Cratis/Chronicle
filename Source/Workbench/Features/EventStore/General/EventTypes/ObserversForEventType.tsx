// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { GetObserversForEventType, ObserverInformationForEventType, ObserverType } from 'Api/Observation';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { getObserverRunningStateAsText } from '../../Namespaces/Observers/getObserverRunningStateAsText';
import strings from 'Strings';

/**
 * Props for {@link ObserversForEventType}.
 */
export interface ObserversForEventTypeProps {
    /**
     * The identifier of the event type to list consuming observers for.
     */
    eventTypeId: string;
}

const renderObserverType = (row: ObserverInformationForEventType) => {
    switch (row.observer.type) {
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

const renderRunningState = (row: ObserverInformationForEventType) =>
    getObserverRunningStateAsText(row.observer.runningState);

/**
 * Lists all observers across all namespaces in the current event store that consume the given event type.
 *
 * @param props - The {@link ObserversForEventTypeProps}.
 */
export const ObserversForEventType = ({ eventTypeId }: ObserversForEventTypeProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [observersQuery, performObserversQuery] = GetObserversForEventType.use({
        eventStore: params.eventStore!,
        eventTypeId
    });

    useEffect(() => {
        performObserversQuery({
            eventStore: params.eventStore!,
            eventTypeId
        });
    }, [eventTypeId, params.eventStore]);

    return (
        <DataTable
            value={observersQuery.data ?? []}
            dataKey='observer.id'
            emptyMessage={strings.eventStore.general.eventTypes.observers.empty}
            scrollable
            scrollHeight='flex'
            style={{ height: '100%' }}>
            <Column
                field='namespace'
                header={strings.eventStore.general.eventTypes.observers.columns.namespace}
                sortable />
            <Column
                field='observer.id'
                header={strings.eventStore.general.eventTypes.observers.columns.id}
                sortable />
            <Column
                field='observer.type'
                header={strings.eventStore.general.eventTypes.observers.columns.observerType}
                body={renderObserverType}
                sortable />
            <Column
                field='observer.runningState'
                header={strings.eventStore.general.eventTypes.observers.columns.state}
                body={renderRunningState}
                sortable />
        </DataTable>
    );
};
