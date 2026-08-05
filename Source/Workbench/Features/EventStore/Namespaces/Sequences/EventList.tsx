// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { Dropdown } from 'primereact/dropdown';
import { IconField } from 'primereact/iconfield';
import { InputIcon } from 'primereact/inputicon';
import { InputText } from 'primereact/inputtext';
import { useDebounceValue } from 'usehooks-ts';
import { DataTableForQuery } from '@cratis/components/DataTables';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { AppendedEvent } from 'Api/Events';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { getDistinctEventTypeOptions } from './getDistinctEventTypeOptions';
import css from './EventList.module.css';

const filterStrings = strings.eventStore.namespaces.sequences.filters;

const occurred = (event: AppendedEvent) => {
    return event.context.occurred.toLocaleString();
};

// Column filters run client-side over the events currently loaded on the page (DataTableForQuery loads 20 at
// a time), so they only narrow the loaded page — not the whole sequence. The event source id input is the
// whole-sequence, server-side filter fed straight into the AppendedEvents query.
const defaultFilters: DataTableFilterMeta = {
    'context.eventType.id': { value: null, matchMode: FilterMatchMode.EQUALS },
};

export const EventList = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [eventSourceId, setEventSourceId] = useState('');
    const [debouncedEventSourceId, setDebouncedEventSourceId] = useDebounceValue('', 300);

    const [eventTypes] = AllEventTypes.use({ eventStore: params.eventStore! });
    const eventTypeOptions = getDistinctEventTypeOptions(eventTypes.data);

    const handleEventSourceIdChange = (value: string) => {
        setEventSourceId(value);
        setDebouncedEventSourceId(value);
    };

    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log',
        eventSourceId: debouncedEventSourceId.trim() || undefined
    };

    const eventTypeFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
        <Dropdown
            value={options.value}
            options={eventTypeOptions}
            onChange={(e) => options.filterCallback(e.value)}
            optionLabel='label'
            placeholder={filterStrings.placeholders.eventType}
            showClear
            className='p-column-filter' />
    );

    return (
        <div className={css.eventList}>
            <div className={css.filters}>
                <IconField iconPosition='left' className={css.searchField}>
                    <InputIcon className='pi pi-search' />
                    <InputText
                        value={eventSourceId}
                        onChange={(e) => handleEventSourceIdChange(e.target.value)}
                        placeholder={filterStrings.eventSourceId.placeholder}
                        className='w-full' />
                </IconField>
                <small className={css.note}>{filterStrings.eventSourceId.help}</small>
                <small className={css.note}>{filterStrings.pageScopedNote}</small>
            </div>
            <div className={css.table}>
                <DataTableForQuery
                    query={AppendedEvents}
                    queryArguments={queryArgs}
                    emptyMessage={strings.eventStore.namespaces.sequences.empty}
                    dataKey='context.sequenceNumber'
                    defaultFilters={defaultFilters}
                    globalFilterFields={['context.eventType.id']}
                    clientFiltering>
                    <Column field='context.sequenceNumber' header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />
                    <Column
                        field='context.eventType.id'
                        header={strings.eventStore.namespaces.sequences.columns.eventType}
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='context.eventType.id'
                        filterElement={eventTypeFilterTemplate} />
                    <Column field='context.occurred' header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred} />
                </DataTableForQuery>
            </div>
        </div>
    );
};
