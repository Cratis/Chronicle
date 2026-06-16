// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import strings from 'Strings';
import { AllEventTypesParameters, AllEventTypesWithSchemas } from 'Api/EventTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { FilterMatchMode } from 'primereact/api';
import { DataTable, DataTableFilterMeta } from 'primereact/datatable';
import { Page } from 'Components/Common/Page';
import { TypeDetails } from './TypeDetails';
import * as faIcons from 'react-icons/fa6';
import { EventTypeOwner, EventTypeRegistration, EventTypeSource } from 'Api/Events';
import { useState, useCallback, useMemo } from 'react';
import { Dropdown } from 'primereact/dropdown';
import { DialogResult, useDialog } from '@cratis/arc.react/dialogs';
import { AddEventTypeDialog } from './AddEventTypeDialog';
import { Menubar } from 'primereact/menubar';
import { Allotment } from 'allotment';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
    owner: { value: null, matchMode: FilterMatchMode.EQUALS },
    source: { value: null, matchMode: FilterMatchMode.EQUALS }
};

const renderTombstone = () => {
    return 'no';
};

const renderSource = (eventType: EventTypeRegistration) => {
    switch (eventType.source) {
        case EventTypeSource.code:
            return strings.eventStore.general.eventTypes.sources.code;
        case EventTypeSource.user:
            return strings.eventStore.general.eventTypes.sources.user;
    }
    return strings.eventStore.general.eventTypes.sources.unknown;
};

const renderOwner = (eventType: EventTypeRegistration) => {
    switch (eventType.owner) {
        case EventTypeOwner.client:
            return strings.eventStore.general.eventTypes.owners.client;
        case EventTypeOwner.server:
            return strings.eventStore.general.eventTypes.owners.server;
    }
    return strings.eventStore.general.eventTypes.owners.unknown;
};

const ownerFilterOptions = [
    { label: strings.eventStore.general.eventTypes.owners.client, value: EventTypeOwner.client },
    { label: strings.eventStore.general.eventTypes.owners.server, value: EventTypeOwner.server }
];

const sourceFilterOptions = [
    { label: strings.eventStore.general.eventTypes.sources.code, value: EventTypeSource.code },
    { label: strings.eventStore.general.eventTypes.sources.user, value: EventTypeSource.user }
];

const ownerFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
    <Dropdown
        value={options.value}
        options={ownerFilterOptions}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

const sourceFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
    <Dropdown
        value={options.value}
        options={sourceFilterOptions}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

export const EventTypes = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddEventTypeDialogWrapper, showAddEventTypeDialog] = useDialog(AddEventTypeDialog);
    // TODO: This is a workaround to force refresh after save. Should be replaced with WebSocket-based updates.
    const [refreshTrigger, setRefreshTrigger] = useState(0);
    const [selectedItem, setSelectedItem] = useState<EventTypeRegistration | undefined>(undefined);
    const [filters, setFilters] = useState<DataTableFilterMeta>(defaultFilters);

    const queryArgs: AllEventTypesParameters = {
        eventStore: params.eventStore!
    };

    // Use the non-paging query to load all event types
    // Note: refreshTrigger is used as a dependency to force refetch
    const [result] = AllEventTypesWithSchemas.use(queryArgs);

    const handleAddEventType = useCallback(async () => {
        const [result] = await showAddEventTypeDialog();
        if (result === DialogResult.Ok) {
            setTimeout(() => setRefreshTrigger(prev => prev + 1), 200);
        }
    }, [showAddEventTypeDialog]);

    const menuItems = useMemo(() => [
        {
            label: strings.eventStore.general.eventTypes.actions.create,
            icon: <faIcons.FaPlus className='mr-2' />,
            command: handleAddEventType
        }
    ], [handleAddEventType]);

    // Show loading or error state
    if (result.isPerforming) {
        return (
            <Page title={strings.eventStore.general.eventTypes.title}>
                <div className="flex items-center justify-center h-full">
                    <div>Loading event types...</div>
                </div>
            </Page>
        );
    }

    if (result.hasExceptions) {
        return (
            <Page title={strings.eventStore.general.eventTypes.title}>
                <div className="flex items-center justify-center h-full">
                    <div className="text-red-500">Error loading event types: {result.exceptionMessages.join(', ')}</div>
                </div>
            </Page>
        );
    }

    return (
        <Page title={strings.eventStore.general.eventTypes.title}>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane className="flex-grow">
                    <div className="px-4 py-2">
                        <Menubar
                            aria-label="Actions"
                            model={menuItems}
                        />
                    </div>
                    <div className="flex flex-col border border-cratis-surface-border rounded mx-4 mb-4 overflow-hidden"
                         style={{ height: 'calc(100% - 76px)' }}>
                        <DataTable
                            key={refreshTrigger}
                            value={result.data}
                            scrollable
                            scrollHeight='flex'
                            selectionMode='single'
                            selection={selectedItem}
                            onSelectionChange={(e) => setSelectedItem(e.value as EventTypeRegistration)}
                            dataKey='type.id'
                            filters={filters}
                            filterDisplay='menu'
                            onFilter={(e) => setFilters(e.filters)}
                            globalFilterFields={['tombstone']}
                            emptyMessage={strings.eventStore.general.eventTypes.empty}>

                            <Column field='type.id' header={strings.eventStore.general.eventTypes.columns.name} sortable />
                            <Column
                                field='owner'
                                style={{ width: '100px' }}
                                header={strings.eventStore.general.eventTypes.columns.owner}
                                showFilterMatchModes={false}
                                filter
                                filterMenuStyle={{ width: '14rem' }}
                                filterField='owner'
                                filterElement={ownerFilterTemplate}
                                body={renderOwner}
                                sortable />
                            <Column
                                field='source'
                                style={{ width: '100px' }}
                                header={strings.eventStore.general.eventTypes.columns.source}
                                showFilterMatchModes={false}
                                filter
                                filterMenuStyle={{ width: '14rem' }}
                                filterField='source'
                                filterElement={sourceFilterTemplate}
                                body={renderSource}
                                sortable />
                            <Column
                                field='type.generation'
                                style={{ width: '100px' }}
                                header={strings.eventStore.general.eventTypes.columns.generation}
                                sortable />
                            <Column
                                field='tombstone'
                                style={{ width: '100px' }}
                                header={strings.eventStore.general.eventTypes.columns.tombstone}
                                body={renderTombstone}
                                sortable />
                        </DataTable>
                    </div>
                </Allotment.Pane>
                {selectedItem &&
                    <Allotment.Pane preferredSize="450px">
                        <TypeDetails item={selectedItem} />
                    </Allotment.Pane>
                }
            </Allotment>
            <AddEventTypeDialogWrapper />
        </Page>
    );
};
