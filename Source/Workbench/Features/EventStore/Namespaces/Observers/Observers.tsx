// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import { DataTable, DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { Dropdown } from 'primereact/dropdown';
import { Menubar } from 'primereact/menubar';
import { IconField } from 'primereact/iconfield';
import { InputIcon } from 'primereact/inputicon';
import { InputText } from 'primereact/inputtext';
import { ObserverType } from 'Api/Observation/ObserverType';
import { ObserverInformation } from 'Api/Observation/ObserverInformation';
import { ObserverRunningState } from 'Api/Observation/ObserverRunningState';
import strings from 'Strings';
import { AllObservers, AllObserversParameters, ObserverOwner } from 'Api/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Page } from 'Components/Common/Page';
import * as faIcons from 'react-icons/fa6';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import { ObserverSequenceType } from './ObserverSequenceType';

const legacyEventLogSequenceId = '00000000-0000-0000-0000-000000000000';

const observerSequenceTypeOrder = [
    ObserverSequenceType.eventLog,
    ObserverSequenceType.system,
    ObserverSequenceType.outbox,
    ObserverSequenceType.inbox,
    ObserverSequenceType.custom,
];

const getObserverSequenceType = (eventSequenceId: string) => {
    switch (eventSequenceId) {
        case '':
        case 'event-log':
        case legacyEventLogSequenceId:
            return ObserverSequenceType.eventLog;
        case 'system':
            return ObserverSequenceType.system;
        case 'outbox':
            return ObserverSequenceType.outbox;
        case 'inbox':
            return ObserverSequenceType.inbox;
    }

    if (eventSequenceId.startsWith('inbox-')) {
        return ObserverSequenceType.inbox;
    }

    return ObserverSequenceType.custom;
};

const getObserverSequenceTypeLabel = (sequenceType: ObserverSequenceType) => {
    switch (sequenceType) {
        case ObserverSequenceType.eventLog:
            return strings.eventStore.namespaces.observers.sequenceTypes.eventLog;
        case ObserverSequenceType.system:
            return strings.eventStore.namespaces.observers.sequenceTypes.system;
        case ObserverSequenceType.outbox:
            return strings.eventStore.namespaces.observers.sequenceTypes.outbox;
        case ObserverSequenceType.inbox:
            return strings.eventStore.namespaces.observers.sequenceTypes.inbox;
        case ObserverSequenceType.custom:
            return strings.eventStore.namespaces.observers.sequenceTypes.custom;
    }
};

const observerType = (observer: ObserverInformation) => {
    switch (observer.type) {
        case ObserverType.unknown:
            return strings.eventStore.namespaces.observers.types.unknown;
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

const observerOwner = (observer: ObserverInformation) => {
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

const runningState = (observer: ObserverInformation) => {
    return getObserverRunningStateAsText(observer.runningState);
};

const observerTypeFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
    <Dropdown
        value={options.value}
        options={[
            { label: strings.eventStore.namespaces.observers.types.unknown, value: ObserverType.unknown },
            { label: strings.eventStore.namespaces.observers.types.reactor, value: ObserverType.reactor },
            { label: strings.eventStore.namespaces.observers.types.projection, value: ObserverType.projection },
            { label: strings.eventStore.namespaces.observers.types.reducer, value: ObserverType.reducer },
            { label: strings.eventStore.namespaces.observers.types.external, value: ObserverType.external },
        ]}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

const observerOwnerFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
    <Dropdown
        value={options.value}
        options={[
            { label: strings.eventStore.namespaces.observers.owners.none, value: ObserverOwner.none },
            { label: strings.eventStore.namespaces.observers.owners.client, value: ObserverOwner.client },
            { label: strings.eventStore.namespaces.observers.owners.kernel, value: ObserverOwner.kernel },
        ]}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

const runningStateFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
    <Dropdown
        value={options.value}
        options={[
            { label: strings.eventStore.namespaces.observers.states.unknown, value: ObserverRunningState.unknown },
            { label: strings.eventStore.namespaces.observers.states.active, value: ObserverRunningState.active },
            { label: strings.eventStore.namespaces.observers.states.suspended, value: ObserverRunningState.suspended },
            { label: strings.eventStore.namespaces.observers.states.replaying, value: ObserverRunningState.replaying },
            { label: strings.eventStore.namespaces.observers.states.disconnected, value: ObserverRunningState.disconnected },
            { label: strings.eventStore.namespaces.observers.states.quarantined, value: ObserverRunningState.quarantined },
        ]}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [searchText, setSearchText] = useState('');
    const [filters, setFilters] = useState<DataTableFilterMeta>({
        global: { value: null, matchMode: FilterMatchMode.CONTAINS },
        type: { value: null, matchMode: FilterMatchMode.EQUALS },
        owner: { value: null, matchMode: FilterMatchMode.EQUALS },
        runningState: { value: null, matchMode: FilterMatchMode.EQUALS },
        sequenceType: { value: null, matchMode: FilterMatchMode.EQUALS },
    });

    const queryArgs: AllObserversParameters = {
        eventStore: params.eventStore!,
        namespace: viewModel.currentNamespace
    };

    const [observers] = AllObservers.when(!!viewModel.currentNamespace).use(queryArgs);

    const observerRows = (observers.data ?? []).map(observer => ({
        ...observer,
        sequenceType: getObserverSequenceType(observer.eventSequenceId),
    }));

    const observerSequenceTypeOptions = observerSequenceTypeOrder
        .filter(sequenceType => observerRows.some(observer => observer.sequenceType === sequenceType))
        .map(sequenceType => ({
            label: getObserverSequenceTypeLabel(sequenceType),
            value: sequenceType,
        }));

    const handleSearch = (value: string) => {
        setSearchText(value);
        setFilters(prev => ({
            ...prev,
            global: { value: value || null, matchMode: FilterMatchMode.CONTAINS }
        }));
    };

    const menuItems = [
        {
            label: strings.eventStore.namespaces.observers.actions.replay,
            icon: <faIcons.FaArrowsRotate className='mr-2' />,
            disabled: !viewModel.selectedObserver,
            command: () => viewModel.replay()
        }
    ];

    const searchInput = (
        <IconField iconPosition='left'>
            <InputIcon className='pi pi-search' />
            <InputText
                value={searchText}
                onChange={(e) => handleSearch(e.target.value)}
                placeholder={strings.eventStore.namespaces.observers.search}
            />
        </IconField>
    );

    const observerSequenceTypeFilterTemplate = (options: ColumnFilterElementTemplateOptions) => (
        <Dropdown
            value={options.value}
            options={observerSequenceTypeOptions}
            onChange={(e) => options.filterCallback(e.value)}
            optionLabel='label'
            placeholder='All'
            showClear
            className='p-column-filter'
        />
    );

    return (
        <Page title={strings.eventStore.namespaces.observers.title}>
            <div className='px-4 py-2'>
                <Menubar model={menuItems} end={searchInput} />
            </div>
            <div className='flex-1 overflow-hidden px-4 pb-4'>
                <DataTable
                    value={observerRows}
                    selectionMode='single'
                    selection={viewModel.selectedObserver}
                    onSelectionChange={(e) => (viewModel.selectedObserver = e.value as ObserverInformation)}
                    dataKey='id'
                    filters={filters}
                    filterDisplay='menu'
                    onFilter={(e) => setFilters(e.filters)}
                    globalFilterFields={['id', 'eventSequenceId']}
                    emptyMessage={strings.eventStore.namespaces.observers.empty}
                    scrollable
                    scrollHeight='flex'
                    style={{ height: '100%' }}>
                    <Column field='id' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                    <Column
                        field='eventSequenceId'
                        header={strings.eventStore.namespaces.observers.columns.sequence}
                        sortable
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='sequenceType'
                        filterElement={observerSequenceTypeFilterTemplate} />
                    <Column
                        field='type'
                        header={strings.eventStore.namespaces.observers.columns.observerType}
                        sortable
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='type'
                        filterElement={observerTypeFilterTemplate}
                        body={observerType} />
                    <Column
                        field='owner'
                        header={strings.eventStore.namespaces.observers.columns.owner}
                        sortable
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='owner'
                        filterElement={observerOwnerFilterTemplate}
                        body={observerOwner} />
                    <Column
                        field='nextEventSequenceNumber'
                        dataType='numeric'
                        header={strings.eventStore.namespaces.observers.columns.nextEventSequenceNumber}
                        sortable />
                    <Column
                        field='runningState'
                        dataType='numeric'
                        header={strings.eventStore.namespaces.observers.columns.state}
                        sortable
                        showFilterMatchModes={false}
                        filter
                        filterMenuStyle={{ width: '14rem' }}
                        filterField='runningState'
                        filterElement={runningStateFilterTemplate}
                        body={runningState} />
                </DataTable>
            </div>
        </Page>
    );
});
