// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage } from 'Components';
import strings from 'Strings';
import { AppendedEvents, AppendedEventsArguments, AppendedEventWithJsonAsContent } from 'Api/EventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column } from 'primereact/column';

const occurred = (event: AppendedEventWithJsonAsContent) => {
    return event.context.occurred.toLocaleString();
};

export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AppendedEventsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.sequences.title}
            query={AppendedEvents}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.sequences.empty}
            dataKey='metadata.sequenceNumber'>
            <DataPage.Columns>
                <Column field='metadata.sequenceNumber' header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />
                <Column field='metadata.type.id' header={strings.eventStore.namespaces.sequences.columns.eventType} />
                <Column field='context.occurred' header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>
    );
};
