// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllFailedPartitions, AllFailedPartitionsParameters } from 'Features/Observation';
import { type DataTableFilterMeta } from '@cratis/components/DataTables';
import { FilterMatchMode } from '@primereact/headless/datatable';
import { Tooltip } from '@cratis/components/Common';
import { Tag } from '@cratis/components/Display';
import { getFailedPartitionStatus } from './getFailedPartitionStatus';
import { getFailedPartitionStatusLabel } from './getFailedPartitionStatusLabel';
import { getFailedPartitionSeverity } from './getFailedPartitionSeverity';
import { useParams, useSearchParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { FailedPartitionsViewModel } from './FailedPartitionsViewModel';
import { FailedPartitionDetails } from './FailedPartitionDetails';
import { getFailedPartitionErrorGlimpse } from './getFailedPartitionErrorGlimpse';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import * as faIcons from 'react-icons/fa6';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.In },
};

const partition = (failedPartition: FailedPartition) => {
    return Object.values(failedPartition.partition).join('');
};

const partitionColumnBody = (failedPartition: FailedPartition) => {
    const glimpse = getFailedPartitionErrorGlimpse(failedPartition);
    return (
        <Tooltip content={glimpse} position='bottom'>
            <span>{partition(failedPartition)}</span>
        </Tooltip>
    );
};

const statusColumnBody = (failedPartition: FailedPartition) => {
    const status = getFailedPartitionStatus(failedPartition);
    return <Tag value={getFailedPartitionStatusLabel(status)} severity={getFailedPartitionSeverity(status)} />;
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
    const [searchParams, setSearchParams] = useSearchParams();
    const observerId = searchParams.get('observerId') ?? undefined;

    const queryArgs: AllFailedPartitionsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        observerId
    };

    const clearObserverFilter = () => {
        const next = new URLSearchParams(searchParams);
        next.delete('observerId');
        setSearchParams(next);
    };

    return (
        <Page title={strings.eventStore.namespaces.failedPartitions.title}>
        {/* A filtered list that does not say it is filtered is how people conclude their partitions
            have disappeared. */}
        {observerId &&
            <div className='px-4 py-2 flex items-center gap-2'>
                <span>{strings.eventStore.namespaces.failedPartitions.filteredByObserver.replace('{observerId}', observerId)}</span>
                <button type='button' className='underline' onClick={clearObserverFilter}>
                    {strings.eventStore.namespaces.failedPartitions.clearObserverFilter}
                </button>
            </div>}
        <DataPage
            key={observerId ?? ''}
            title={strings.eventStore.namespaces.failedPartitions.title}
            query={AllFailedPartitions}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedFailedPartition = e.value as FailedPartition)}
            dataKey='id'
            detailsComponent={FailedPartitionDetails}
            defaultFilters={defaultFilters}
            globalFilterFields={['tombstone']}
            emptyMessage={strings.eventStore.namespaces.failedPartitions.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    label={strings.eventStore.namespaces.failedPartitions.actions.retry}
                    icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.retry()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column field='isQuarantined' header={strings.eventStore.namespaces.failedPartitions.columns.status} body={statusColumnBody} />
                <Column field='partition' header={strings.eventStore.namespaces.failedPartitions.columns.partition} sortable body={partitionColumnBody} />
                <Column field='attempts' header={strings.eventStore.namespaces.failedPartitions.columns.attempts} sortable body={attempts} />
                <Column field='lastAttempt' header={strings.eventStore.namespaces.failedPartitions.columns.lastAttempt} sortable body={lastAttempt} />
            </DataPage.Columns>
        </DataPage>
        </Page>
    );
});
