// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from 'MVVM';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable, DataTableFilterMeta } from 'primereact/datatable';
import {
    Column,
    ColumnFilterApplyTemplateOptions,
    ColumnFilterClearTemplateOptions,
    ColumnFilterElementTemplateOptions
} from 'primereact/column';
import { ObserverRunningState } from 'API/events/store/observers/ObserverRunningState';
import { ObserverType } from 'API/events/store/observers/ObserverType';
import { Page } from '../../../Page';
import { ObserverState } from 'API/events/store/observers/ObserverState';
import { Toolbar } from 'primereact/toolbar';
import { Button } from 'primereact/button';
import * as mdIcons from 'react-icons/md';
import { FilterMatchMode } from 'primereact/api';
import { MultiSelect, MultiSelectChangeEvent } from 'primereact/multiselect';
import { useState } from 'react';

const observerType = (observer: ObserverState) => {
    switch (observer.type) {
        case ObserverType.unknown: return 'Unknown';
        case ObserverType.client: return 'Client';
        case ObserverType.projection: return 'Projection';
        case ObserverType.inbox: return 'Inbox';
        case ObserverType.reducer: return 'Reducer';
    }
    return '[N/A]';
}

const getObserverRunningStateAsText = (runningState: ObserverRunningState) => {
    switch (runningState) {
        case ObserverRunningState.new: return 'New';
        case ObserverRunningState.subscribing: return 'Subscribing';
        case ObserverRunningState.rewinding: return 'Rewinding';
        case ObserverRunningState.replaying: return 'Replaying';
        case ObserverRunningState.catchingUp: return 'CatchingUp';
        case ObserverRunningState.active: return 'Active';
        case ObserverRunningState.paused: return 'Paused';
        case ObserverRunningState.stopped: return 'Stopped';
        case ObserverRunningState.suspended: return 'Suspended';
        case ObserverRunningState.failed: return 'Failed';
        case ObserverRunningState.tailOfReplay: return 'TailOfReplay';
        case ObserverRunningState.disconnected: return 'Disconnected';
    }
    return '[N/A]';
}

const defaultFilters: DataTableFilterMeta = {
    runningState: { value: null, matchMode: FilterMatchMode.IN }
};

interface ObserverRunningStateOption {
    name: string;
    value: ObserverRunningState | string;
}

const observerRunningStates = Object.values(ObserverRunningState).filter(_ => typeof _ === 'number').map<ObserverRunningStateOption>(_ => ({ name: getObserverRunningStateAsText(_), value: _ }));
const observerRunningStateFilterTemplate = (options: ColumnFilterElementTemplateOptions) => {
    return (
        <MultiSelect
            value={options.value}
            options={observerRunningStates}
            itemTemplate={(option) => option.name}
            onChange={(e: MultiSelectChangeEvent) => {
                options.filterCallback(e.value);
            }}
            placeholder="Any"
            optionLabel="name"
        />
    )
};

const filterClearTemplate = (options: ColumnFilterClearTemplateOptions) => {
    return <Button type="button" icon="pi pi-times" onClick={options.filterClearCallback} severity="secondary"></Button>;
};

const filterApplyTemplate = (options: ColumnFilterApplyTemplateOptions) => {
    return <Button type="button" icon="pi pi-check" onClick={options.filterApplyCallback} severity="success"></Button>;
};



export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(defaultFilters);

    const toolbar = () => {
        return (
            <>
                {viewModel.selectedObserver &&
                    <Button label="Replay" icon={mdIcons.MdReplay} className="mr-2" />
                }
            </>
        )
    };

    return (
        <Page title='Observers'>
            <Toolbar aria-label='Actions' start={toolbar} />

            <DataTable
                value={viewModel.observers}
                rows={100}
                paginator
                alwaysShowPaginator={false}
                scrollable
                selectionMode="single"
                selection={viewModel.selectedObserver}
                onSelectionChange={(e) => viewModel.selectedObserver = e.value as ObserverState}
                dataKey="id"
                filters={filters}
                filterDisplay="menu"
                onFilter={(e) => setFilters(e.filters)}
                globalFilterFields={["name", "type", "runningState"]}
                emptyMessage="No observers found"
            >

                <Column field="observerId" header="Id" sortable />
                <Column field="name" header="Name" sortable />
                <Column field="type" header="ObserverType" sortable body={observerType} />
                <Column
                    field="nextEventSequenceNumber"
                    dataType="numeric"
                    header="Next event sequence number"
                    sortable
                />
                <Column
                    field="runningState"
                    header="State"
                    sortable
                    body={(data: ObserverState) => getObserverRunningStateAsText(data.runningState)}
                    filter
                    filterApply={filterApplyTemplate}
                    filterClear={filterClearTemplate}
                    filterField="runningState"
                    filterElement={observerRunningStateFilterTemplate}
                    showFilterMatchModes={false}
                />
            </DataTable>
        </Page>
    )
});
