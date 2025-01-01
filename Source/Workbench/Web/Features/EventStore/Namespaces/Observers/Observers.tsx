// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/applications.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTableFilterMeta } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { ObserverType } from 'Api/Contracts/Observation/ObserverType';
import { ObserverInformation } from 'Api/Contracts/Observation/ObserverInformation';
import { FilterMatchMode } from 'primereact/api';
import strings from 'Strings';
import { AllObservers, AllObserversArguments } from 'Api/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { ObserverRunningState } from 'Api/Contracts/Observation';

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
    }
    return strings.eventStore.namespaces.observers.types.unknown;
};

const runningState = (observer: ObserverInformation) => {
    switch (observer.runningState) {
        case ObserverRunningState.new:
            return strings.eventStore.namespaces.observers.states.new;
        case ObserverRunningState.routing:
            return strings.eventStore.namespaces.observers.states.routing;
        case ObserverRunningState.replaying:
            return strings.eventStore.namespaces.observers.states.replaying;
        case ObserverRunningState.catchingUp:
            return strings.eventStore.namespaces.observers.states.catchingUp;
        case ObserverRunningState.active:
            return strings.eventStore.namespaces.observers.states.active;
        case ObserverRunningState.paused:
            return strings.eventStore.namespaces.observers.states.paused;
        case ObserverRunningState.stopped:
            return strings.eventStore.namespaces.observers.states.stopped;
        case ObserverRunningState.suspended:
            return strings.eventStore.namespaces.observers.states.suspended;
        case ObserverRunningState.failed:
            return strings.eventStore.namespaces.observers.states.failed;
        case ObserverRunningState.tailOfReplay:
            return strings.eventStore.namespaces.observers.states.tailOfReplay;
        case ObserverRunningState.disconnected:
            return strings.eventStore.namespaces.observers.states.disconnected;
        case ObserverRunningState.indexing:
            return strings.eventStore.namespaces.observers.states.indexing;
    }
    return strings.eventStore.namespaces.observers.states.unknown;
};

const defaultFilters: DataTableFilterMeta = {
    runningState: { value: null, matchMode: FilterMatchMode.IN },
};

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllObserversArguments = {
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
            globalFilterFields={['runningState']}
            dataKey='observerId'
            onSelectionChange={(e) => (viewModel.selectedObserver = e.value as ObserverInformation)}>
            <DataPage.MenuItems>
                <MenuItem
                    id="replay"
                    label={strings.eventStore.namespaces.observers.actions.replay} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.replay()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='observerId' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                <Column
                    field='type'
                    header={strings.eventStore.namespaces.observers.columns.observerType}
                    sortable
                    body={observerType} />

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
                    body={runningState}/>
            </DataPage.Columns>
        </DataPage>
    );
});
