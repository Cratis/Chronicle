// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { DataTableForObservableQuery } from 'Components/DataTables';
import { AllFailedPartitions, AllFailedPartitionsArguments } from 'Api/Observation';
import { Column } from 'primereact/column';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { FailedPartition } from 'Api/Concepts/Observation';
import { withViewModel } from '@cratis/applications.react.mvvm';
import { FailedPartitionsViewModel } from './FailedPartitionsViewModel';
import { Menubar } from 'primereact/menubar';
import { MenuItem } from 'primereact/menuitem';
import { FaArrowsRotate } from 'react-icons/fa6';


const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const partition = (failedPartition: FailedPartition) => {
    return Object.values(failedPartition.partition.value).join('');
};

const attempts = (failedPartition: FailedPartition) => {
    return failedPartition.attempts.length;
};

const lastAttempt = (failedPartition: FailedPartition) => {
    return failedPartition.lastAttempt.occurred.toLocaleString();
};

export const FailedPartitions = withViewModel(FailedPartitionsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AllFailedPartitionsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    const hasSelectedFailedPartition = viewModel.selectedFailedPartition !== undefined;

    const menuItems: MenuItem[] = [
        {
            id: 'replay',
            label: strings.eventStore.namespaces.failedPartitions.actions.retry,
            icon: <FaArrowsRotate className={'mr-2'} />,
            disabled: !hasSelectedFailedPartition,
            command: () => viewModel.retry()
        }
    ];

    return (
        <Page title={strings.eventStore.namespaces.failedPartitions.title}>

            <div className="px-4 py-2">
                <Menubar aria-label='Actions' model={menuItems} />
            </div>


            <div className={'flex-1 overflow-hidden'}>
                <DataTableForObservableQuery
                    query={AllFailedPartitions}
                    queryArguments={queryArgs}
                    selection={viewModel.selectedFailedPartition}
                    onSelectionChange={(e) => (viewModel.selectedFailedPartition = e.value as FailedPartition)}
                    dataKey='id'
                    defaultFilters={defaultFilters}
                    globalFilterFields={['tombstone']}
                    emptyMessage='No failed partitions'>
                    <Column field='partition' header={strings.eventStore.namespaces.failedPartitions.columns.partition} sortable body={partition} />
                    <Column field='attempts' header={strings.eventStore.namespaces.failedPartitions.columns.attempts} sortable body={attempts} />
                    <Column field='lastAttempt' header={strings.eventStore.namespaces.failedPartitions.columns.lastAttempt} sortable body={lastAttempt} />
                </DataTableForObservableQuery>
            </div>

        </Page>
    );
});
