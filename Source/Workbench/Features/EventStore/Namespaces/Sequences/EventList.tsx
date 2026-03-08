// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTableForQuery } from 'Components/index';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { AppendedEvent } from 'Api/Events';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { useParams } from 'react-router-dom';

export interface EventListProps {
    events: object[];
}

const occurred = (event: AppendedEvent) => {
    return event.context.occurred.toLocaleString();
};

export const EventList = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    return (
        <DataTableForQuery
            query={AppendedEvents}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.sequences.empty}
            dataKey='metadata.sequenceNumber'>
            <Column field='metadata.sequenceNumber' header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />
            <Column field='metadata.type.id' header={strings.eventStore.namespaces.sequences.columns.eventType} />
            <Column field='context.occurred' header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred} />
        </DataTableForQuery>
    );
};
