// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllFailedPartitions, AllFailedPartitionsParameters } from 'Api/Observation';
import { Column } from 'primereact/column';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { FailedPartition } from 'Api/Observation';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { FailedPartitionsViewModel } from './FailedPartitionsViewModel';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const partition = (failedPartition: FailedPartition) => {
    return Object.values(failedPartition.partition).join('');
};

const attempts = (failedPartition: FailedPartition) => {
    return failedPartition.attempts.length;
};

const lastAttempt = (failedPartition: FailedPartition) => {
    if( failedPartition.attempts.length === 0 ) return '';
    return failedPartition.attempts[failedPartition.attempts.length-1].occurred.toLocaleString();
};

export const FailedPartitions = withViewModel(FailedPartitionsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AllFailedPartitionsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.failedPartitions.title}
            query={AllFailedPartitions}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedFailedPartition = e.value as FailedPartition)}
            dataKey='id'
            defaultFilters={defaultFilters}
            globalFilterFields={['tombstone']}
            emptyMessage={strings.eventStore.namespaces.failedPartitions.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    id='retry'
                    label={strings.eventStore.namespaces.failedPartitions.actions.retry}
                    icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.retry()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column field='partition' header={strings.eventStore.namespaces.failedPartitions.columns.partition} sortable body={partition} />
                <Column field='attempts' header={strings.eventStore.namespaces.failedPartitions.columns.attempts} sortable body={attempts} />
                <Column field='lastAttempt' header={strings.eventStore.namespaces.failedPartitions.columns.lastAttempt} sortable body={lastAttempt} />
            </DataPage.Columns>
        </DataPage>
    );
});
