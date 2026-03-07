// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage, MenuItem } from 'Components';
import strings from 'Strings';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import { EventDetails } from './EventDetails';
import { AppendedEvent } from 'Api/Events';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { MultiSelect } from 'primereact/multiselect';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { useDialog, DialogResult } from '@cratis/arc.react/dialogs';
import { AppendEventDialog } from './Add/AppendEventDialog';
import { useState } from 'react';
import { RedactEventDialog, RedactEventDialogProps } from './RedactEventDialog';
import * as faIcons from 'react-icons/fa6';

import { PropertyPathResolverProxyHandler } from '@cratis/fundamentals';

const occurred = (event: AppendedEvent) => {
    return event.context.occurred.toLocaleString();
};

// Delay in milliseconds to wait before refreshing data after adding an event
// This allows the backend to process and persist the event before re-querying
const REFRESH_DELAY_MS = 200;

type Lambda<T> = (target: T) => unknown;

function GetPathFor<T>(lambda: Lambda<T>): string {
    const handler = new PropertyPathResolverProxyHandler();
    const proxy = new Proxy({}, handler);
    lambda(proxy);
    return handler.path;
}

export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AppendEventWrapper, showAppendEvent] = useDialog(AppendEventDialog);
    const [refreshTrigger, setRefreshTrigger] = useState(0);
    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | null>(null);
    const [RedactEventWrapper, showRedactEvent] = useDialog<RedactEventDialogProps>(RedactEventDialog);

    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    const handleRedactEvent = () => {
        if (selectedEvent) {
            showRedactEvent({
                eventStore: params.eventStore!,
                namespace: params.namespace!,
                eventSequenceId: 'event-log',
                sequenceNumber: selectedEvent.context.sequenceNumber
            });
        }
    };

    const sequenceNumberPath = GetPathFor<AppendedEvent>(et => et.context.sequenceNumber);
    const typePath = GetPathFor<AppendedEvent>(et => et.context.eventType.id);
    const eventSourceIdPath = GetPathFor<AppendedEvent>(et => et.context.eventSourceId);
    const occurredPath = GetPathFor<AppendedEvent>(et => et.context.occurred);

    const filters: DataTableFilterMeta = {
        idPath: { value: null, matchMode: FilterMatchMode.IN },
        eventSourceIdPath: { value: null, matchMode: FilterMatchMode.EQUALS },
        occurredPath: { value: null, matchMode: FilterMatchMode.BETWEEN }
    };

    const [eventTypes] = AllEventTypes.use({
        eventStore: params.eventStore!
    });

    const handler = new PropertyPathResolverProxyHandler();
    const proxy = new Proxy({}, handler);
    const accessor = (et: AppendedEvent) => et.context.eventType.id;
    accessor(proxy);

    const handleAppendEvent = async () => {
        const [result] = await showAppendEvent();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), REFRESH_DELAY_MS);
        }
    };

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
        <>
            <DataPage
                key={refreshTrigger}
                title={strings.eventStore.namespaces.sequences.title}
                query={AppendedEvents}
                queryArguments={queryArgs}
                emptyMessage={strings.eventStore.namespaces.sequences.empty}
                dataKey={sequenceNumberPath}
                defaultFilters={filters}
                globalFilterFields={['context.eventType.id']}
                detailsComponent={EventDetails}
                onSelectionChange={(e) => setSelectedEvent(e.value as AppendedEvent | null)}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='appendEvent'
                        label={strings.eventStore.namespaces.sequences.actions.appendEvent}
                        icon={faIcons.FaPlus}
                        command={handleAppendEvent} />
                    <MenuItem
                        id='redactEvent'
                        label={strings.eventStore.namespaces.sequences.actions.redact}
                        icon={faIcons.FaEraser}
                        disableOnUnselected
                        command={handleRedactEvent} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column field={sequenceNumberPath} header={strings.eventStore.namespaces.sequences.columns.sequenceNumber} />

                    <Column
                        field={typePath}
                        header={strings.eventStore.namespaces.sequences.columns.eventType}
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField={typePath}
                        filterElement={eventTypeFilterTemplate}
                        filterPlaceholder={strings.eventStore.namespaces.sequences.filters.placeholders.eventType} />

                    <Column
                        field={eventSourceIdPath}
                        header={strings.eventStore.namespaces.sequences.columns.eventSourceId}
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField={eventSourceIdPath}
                        filterPlaceholder={strings.eventStore.namespaces.sequences.filters.placeholders.eventSourceId} />

                    <Column
                        field={occurredPath}
                        header={strings.eventStore.namespaces.sequences.columns.occurred} body={occurred}
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField={occurredPath}
                        filterPlaceholder={strings.eventStore.namespaces.sequences.filters.placeholders.occurred} />
                </DataPage.Columns>
            </DataPage>
            <AppendEventWrapper />
            <RedactEventWrapper />
        </>
    );
};
