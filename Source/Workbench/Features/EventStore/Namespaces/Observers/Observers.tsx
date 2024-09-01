// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/applications.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable, DataTableFilterMeta } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { ObserverType } from 'Api/Concepts/Observation/ObserverType';
import { Page } from 'Components/Common/Page';
import { ObserverInformation } from 'Api/Concepts/Observation/ObserverInformation';
import { FilterMatchMode } from 'primereact/api';
import { useState } from 'react';
import { ColumnFilterProps } from 'Components/ColumnFilter/ColumnFilter';
import {
    ObserverRunningStateFilterTemplate,
    getObserverRunningStateAsText,
} from './ObserverRunningStateHelpers';
import { Menubar } from 'primereact/menubar';
import { MenuItem } from 'primereact/menuitem';
import { FaArrowsRotate } from "react-icons/fa6";
import strings from 'Strings';

const observerType = (observer: ObserverInformation) => {
    switch (observer.type) {
        case ObserverType.unknown:
            return 'Unknown';
        case ObserverType.client:
            return 'Client';
        case ObserverType.projection:
            return 'Projection';
        case ObserverType.reducer:
            return 'Reducer';
    }
    return '[N/A]';
};

const defaultFilters: DataTableFilterMeta = {
    runningState: { value: null, matchMode: FilterMatchMode.IN },
};

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const [filters, setFilters] = useState<DataTableFilterMeta>(defaultFilters);

    const hasSelectedObserver = viewModel.selectedObserver !== undefined;

    const menuItems: MenuItem[] = [
        {
            id: 'replay',
            label: 'Replay',
            icon: <FaArrowsRotate className={'mr-2'} />,
            disabled: !hasSelectedObserver
        }
    ];

    return (
        <Page title={strings.eventStore.namespaces.observers.title} mainClassName={'overflow-hidden flex flex-col h-full'}>

            <div className="px-4 py-2">
                <Menubar aria-label='Actions' model={menuItems} />
            </div>

            <div className={'flex-1 overflow-hidden'}>
                <DataTable
                    value={viewModel.observers}
                    rows={100}
                    paginator
                    alwaysShowPaginator={false}
                    scrollable
                    scrollHeight={'flex'}
                    selectionMode='single'
                    selection={viewModel.selectedObserver}
                    onSelectionChange={(e) => (viewModel.selectedObserver = e.value as ObserverInformation)}
                    dataKey='observerId'
                    filters={filters}
                    filterDisplay='menu'
                    onFilter={(e) => setFilters(e.filters)}
                    globalFilterFields={['name', 'type', 'runningState']}
                    emptyMessage='No observers found'
                >
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
                        {...ColumnFilterProps}
                        field='runningState'
                        header={strings.eventStore.namespaces.observers.columns.state}
                        sortable
                        body={(data: ObserverInformation) => getObserverRunningStateAsText(data.runningState)}
                        filterElement={ObserverRunningStateFilterTemplate}
                        showFilterMatchModes={false} />
                </DataTable>
            </div>
        </Page>
    );
});
