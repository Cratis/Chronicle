// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from 'MVVM';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable, DataTableFilterMeta } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { ObserverType } from 'API/events/store/observers/ObserverType';
import { Page } from '../../../Page';
import { ObserverState } from 'API/events/store/observers/ObserverState';
import { Toolbar } from 'primereact/toolbar';
import { Button } from 'primereact/button';
import * as mdIcons from 'react-icons/md';
import { FilterMatchMode } from 'primereact/api';
import { useState } from 'react';
import { ColumnFilterProps } from 'Components/ColumnFilter/ColumnFilter';
import { ObserverRunningStateFilterTemplate, getObserverRunningStateAsText } from './ObserverRunningStateHelpers';

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

const defaultFilters: DataTableFilterMeta = {
    runningState: { value: null, matchMode: FilterMatchMode.IN }
};

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(defaultFilters);

    const toolbar = () => {
        return (
            <>
                <Button label="Replay" icon={mdIcons.MdReplay} className="mr-2" disabled={viewModel.selectedObserver == undefined} />
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
                    {...ColumnFilterProps}
                    field="runningState"
                    header="State"
                    sortable
                    body={(data: ObserverState) => getObserverRunningStateAsText(data.runningState)}
                    filterElement={ObserverRunningStateFilterTemplate}
                    showFilterMatchModes={false}
                />
            </DataTable>
        </Page>
    )
});
