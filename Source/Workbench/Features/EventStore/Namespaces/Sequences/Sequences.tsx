// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage, MenuItem } from '@cratis/components/DataPage';
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
import { useDialog, useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { AppendEventDialog } from './Add/AppendEventDialog';
import { useState, useCallback } from 'react';
import { RedactEventDialog, RedactEventDialogProps } from './RedactEventDialog';
import { ReviseDialog, ReviseDialogProps } from './ReviseDialog';
import { GetReplayableObserversForEventTypes } from 'Api/Observation';
import { ObserverType } from 'Api/Observation/ObserverType';
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
    const [showConfirmation] = useConfirmationDialog();
    const [RedactEventWrapper, showRedactEvent] = useDialog<RedactEventDialogProps>(RedactEventDialog);
    const [ReviseWrapper, showRevise] = useDialog<ReviseDialogProps>(ReviseDialog);

    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    const handleRedactEvent = async () => {
        if (selectedEvent) {
            const confirmResult = await showConfirmation(
                strings.eventStore.namespaces.sequences.dialogs.redact.confirmTitle,
                strings.eventStore.namespaces.sequences.dialogs.redact.confirmMessage,
                DialogButtons.YesNo
            );
            if (confirmResult !== DialogResult.Yes) return;

            const [result] = await showRedactEvent({
                eventStore: params.eventStore!,
                namespace: params.namespace!,
                eventSequenceId: 'event-log',
                sequenceNumber: selectedEvent.context.sequenceNumber
            });
            if (result === DialogResult.Ok) {
                setTimeout(() => setRefreshTrigger(prev => prev + 1), REFRESH_DELAY_MS);
            }
        }
    };

    const observerTypeName = (type: ObserverType): string => {
        switch (type) {
            case ObserverType.reactor: return 'Reactor';
            case ObserverType.projection: return 'Projection';
            case ObserverType.reducer: return 'Reducer';
            case ObserverType.external: return 'External';
            default: return 'Unknown';
        }
    };

    const handleReviseEvent = async () => {
        if (selectedEvent) {
            const query = new GetReplayableObserversForEventTypes();
            const queryResult = await query.perform({
                eventStore: params.eventStore!,
                namespace: params.namespace!,
                eventTypeIds: selectedEvent.context.eventType.id
            });

            const observers = queryResult.data;
            const reviseStrings = strings.eventStore.namespaces.sequences.dialogs.revise;
            let confirmMessage: string;

            if (observers.length > 0) {
                const observerList = observers
                    .map(o => `\u2022 ${o.id} (${observerTypeName(o.type)})`)
                    .join('\n');
                confirmMessage = `${reviseStrings.confirmMessage}\n\n${observerList}`;
            } else {
                confirmMessage = reviseStrings.confirmNoObservers;
            }

            const confirmResult = await showConfirmation(
                reviseStrings.confirmTitle,
                confirmMessage,
                DialogButtons.YesNo
            );
            if (confirmResult !== DialogResult.Yes) return;

            const [result] = await showRevise({
                event: selectedEvent,
                eventStore: params.eventStore!,
                namespace: params.namespace!
            });
            if (result === DialogResult.Ok) {
                setTimeout(() => setRefreshTrigger(prev => prev + 1), REFRESH_DELAY_MS);
            }
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

    const [eventsResult] = AppendedEvents.use(queryArgs);

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

    const handleExportEvents = useCallback(() => {
        if (!eventsResult.hasData || eventsResult.data.length === 0) return;

        const sanitize = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '-');

        const exportData = eventsResult.data.map(event => ({
            eventType: event.context.eventType.id,
            eventSourceId: event.context.eventSourceId,
            sequenceNumber: event.context.sequenceNumber,
            occurred: event.context.occurred,
            content: (() => {
                try {
                    return JSON.parse(event.content);
                } catch {
                    return event.content;
                }
            })()
        }));

        const json = JSON.stringify(exportData, null, 2);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `events-${sanitize(params.eventStore!)}-${sanitize(params.namespace!)}-${new Date().toISOString().slice(0, 10)}.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }, [eventsResult.hasData, eventsResult.data, params.eventStore, params.namespace]);

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
                selection={selectedEvent}
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
                    <MenuItem
                        id='reviseEvent'
                        label={strings.eventStore.namespaces.sequences.actions.revise}
                        icon={faIcons.FaArrowsRotate}
                        disableOnUnselected
                        command={handleReviseEvent} />
                    <MenuItem
                        id='exportEvents'
                        label={strings.eventStore.namespaces.sequences.actions.export}
                        icon={faIcons.FaFileExport}
                        command={handleExportEvents} />
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
            <ReviseWrapper />
        </>
    );
};
