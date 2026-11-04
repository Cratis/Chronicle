// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllFailedPartitionGroups, AllFailedPartitionGroupsParameters, FailedPartitionGroup } from 'Features/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Column, DataPage } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { FailedPartitionGroupDetails } from './FailedPartitionGroupDetails';
import { getFailureKindLabel } from './getFailureKindLabel';
import css from './FailedPartitionGroups.module.css';

const kind = (group: FailedPartitionGroup) => getFailureKindLabel(group.kind);
const firstObserved = (group: FailedPartitionGroup) => group.firstObserved?.toLocaleString() ?? '';
const lastObserved = (group: FailedPartitionGroup) => group.lastObserved?.toLocaleString() ?? '';

export const FailedPartitionGroups = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const groupStrings = strings.eventStore.namespaces.failedPartitionGroups;

    const queryArgs: AllFailedPartitionGroupsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <Page title={groupStrings.title}>
            <p className={css.notRootCause}>{groupStrings.notRootCause}</p>
            <DataPage
                title={groupStrings.title}
                query={AllFailedPartitionGroups}
                queryArguments={queryArgs}
                dataKey='id'
                detailsComponent={FailedPartitionGroupDetails}
                emptyMessage={groupStrings.empty}>

                <DataPage.Columns>
                    <Column field='observerId' header={groupStrings.columns.observer} sortable />
                    <Column field='kind' header={groupStrings.columns.kind} sortable body={kind} />
                    <Column field='partitionCount' header={groupStrings.columns.partitions} sortable />
                    <Column field='quarantinedCount' header={groupStrings.columns.quarantined} sortable />
                    <Column field='firstObserved' header={groupStrings.columns.firstObserved} sortable body={firstObserved} />
                    <Column field='lastObserved' header={groupStrings.columns.lastObserved} sortable body={lastObserved} />
                </DataPage.Columns>
            </DataPage>
        </Page>
    );
};
