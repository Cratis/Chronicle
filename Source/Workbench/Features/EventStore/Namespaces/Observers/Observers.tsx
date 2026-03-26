// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { Column, ColumnFilterElementTemplateOptions } from 'primereact/column';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { Dropdown } from 'primereact/dropdown';
import { ObserverType } from 'Api/Observation/ObserverType';
import { ObserverInformation } from 'Api/Observation/ObserverInformation';
import { ObserverRunningState } from 'Api/Observation/ObserverRunningState';
import strings from 'Strings';
import { AllObservers, AllObserversParameters, ObserverOwner } from 'Api/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import * as faIcons from 'react-icons/fa6';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';

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
        ]}
        onChange={(e) => options.filterCallback(e.value)}
        optionLabel='label'
        placeholder='All'
        showClear
        className='p-column-filter'
    />
);

const defaultFilters: DataTableFilterMeta = {
    type: { value: null, matchMode: FilterMatchMode.EQUALS },
    owner: { value: null, matchMode: FilterMatchMode.EQUALS },
    runningState: { value: null, matchMode: FilterMatchMode.EQUALS },
};

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllObserversParameters = {
        eventStore: params.eventStore!,
        namespace: viewModel.currentNamespace
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.observers.title}
            query={AllObservers}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.observers.empty}
            defaultFilters={defaultFilters}
            globalFilterFields={['id', 'eventSequenceId']}
            clientFiltering
            dataKey='id'
            onSelectionChange={(e) => (viewModel.selectedObserver = e.value as ObserverInformation)}>
            <DataPage.MenuItems>
                <MenuItem
                    id="replay"
                    label={strings.eventStore.namespaces.observers.actions.replay} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.replay()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='id' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                <Column field='eventSequenceId' header={strings.eventStore.namespaces.observers.columns.sequence} sortable />
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
            </DataPage.Columns>
        </DataPage>
    );
});
