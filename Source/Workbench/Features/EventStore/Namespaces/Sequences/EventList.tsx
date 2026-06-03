// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTableForQuery } from '@cratis/components/DataTables';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { AppendedEvent } from 'Api/Events';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { useParams } from 'react-router-dom';

export interface EventListFilter {
    eventSourceId?: string;
    eventTypes?: string[];
    startTime?: Date;
    endTime?: Date;
}

export interface EventListProps {
    filter?: EventListFilter;
    eventSequenceId?: string;
    selection?: AppendedEvent | null;
    onSelectionChange?: (event: AppendedEvent | null) => void;
}

const occurred = (event: AppendedEvent) => {
    return event.context.occurred.toLocaleString();
};

export const EventList = (props: EventListProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: props.eventSequenceId ?? 'event-log',
        eventSourceId: props.filter?.eventSourceId,
        eventTypes: props.filter?.eventTypes && props.filter.eventTypes.length > 0
            ? props.filter.eventTypes
            : undefined,
        startTime: props.filter?.startTime as unknown as AppendedEventsParameters['startTime'],
        endTime: props.filter?.endTime as unknown as AppendedEventsParameters['endTime'],
    };

    return (
        <DataTableForQuery
            query={AppendedEvents}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.sequences.empty}
            dataKey='context.sequenceNumber'
            selection={(props.selection ?? null) as unknown as AppendedEvent[] | null}
            onSelectionChange={(event) => props.onSelectionChange?.((event.value as unknown as AppendedEvent | null) ?? null)}>
            <Column field='context.sequenceNumber' header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />
            <Column field='context.eventType.id' header={strings.eventStore.namespaces.sequences.columns.eventType} />
            <Column field='context.occurred' header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred} />
        </DataTableForQuery>
    );
};
