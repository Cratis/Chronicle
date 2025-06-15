// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage } from 'Components';
import strings from 'Strings';
import { AppendedEvents, AppendedEventsArguments } from 'Api/EventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import { EventDetails } from './EventDetails';
import { AppendedEvent } from 'Api/Events';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { MultiSelect } from 'primereact/multiselect';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';

const occurred = (event: AppendedEvent) => {
    return event.context.occurred.toLocaleString();
};

export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AppendedEventsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    const filters:DataTableFilterMeta = {
        'metadata.type.id': { value: null, matchMode: FilterMatchMode.IN }
    };

    const [eventTypes] = AllEventTypes.use({
        eventStore: params.eventStore!
    });

    const eventTypeFilterTemplate = (options: ColumnFilterElementTemplateOptions) => {
        return (
            <MultiSelect
                value={options.value}
                options={eventTypes.data}
                onChange={(e) => options.filterCallback(e.value)}
                optionLabel='id'
                placeholder={strings.eventStore.namespaces.sequences.filters.placeholders.eventType}
                maxSelectedLabels={1}
                className='p-column-filter' />
        );
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.sequences.title}
            query={AppendedEvents}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.sequences.empty}
            dataKey='metadata.sequenceNumber'
            defaultFilters={filters}
            globalFilterFields={['metadata.type.id']}
            detailsComponent={EventDetails}>
            <DataPage.Columns>
                <Column field='metadata.sequenceNumber' header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />
                <Column field='metadata.type.id'
                        header={strings.eventStore.namespaces.sequences.columns.eventType}
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='metadata.type.id'
                        filterElement={eventTypeFilterTemplate}
                        filterPlaceholder='Event Types'/>
                <Column field='context.occurred' header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>
    );
};
