// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ChangeEvent, useState } from 'react';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { Column } from '@cratis/components/DataTables';
import { DataTableCore } from '@cratis/components/DataTables';
import { ActionMenubar, type ActionMenuItem } from '@cratis/components/Common';
import { IconField } from 'primereact/iconfield';
import { InputText } from 'primereact/inputtext';
import { ObserverInformation } from 'Features/Observation';
import strings from 'Strings';
import { AllObservers, AllObserversParameters } from 'Features/Observation';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Page } from 'Components/Common/Page';
import * as faIcons from 'react-icons/fa6';
import { Allotment } from 'allotment';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import { getObserverTypeAsText } from './getObserverTypeAsText';
import { getObserverOwnerAsText } from './getObserverOwnerAsText';
import { ObserverDetails } from './ObserverDetails';
import { ObserverSequenceType } from './ObserverSequenceType';
import { AllFailedPartitions } from 'Features/Observation';
import { ObserverOwner, ObserverRunningState, ObserverType } from 'Features/Contracts/Observation';
import { Link } from 'react-router-dom';

const legacyEventLogSequenceId = '00000000-0000-0000-0000-000000000000';

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

const observerType = (observer: ObserverInformation) => getObserverTypeAsText(observer.type);

const observerOwner = (observer: ObserverInformation) => getObserverOwnerAsText(observer.owner);

const runningState = (observer: ObserverInformation) => {
    return getObserverRunningStateAsText(observer.runningState);
};

/** An observer row, with the values the table derives for display, filtering and sorting. */
type ObserverRow = ObserverInformation & {
    sequenceType: ObserverSequenceType;
    isQuarantined: boolean;
    failedPartitionCount: number;
};

// Nothing at all in the cell when there is nothing to report - the column exists to be scanned, and a
// column of blanks and ticks is scannable in a way that a column of 'no' is not.
const quarantined = (observer: ObserverRow) => observer.isQuarantined
    ? <faIcons.FaCheck aria-label={strings.eventStore.namespaces.observers.columns.quarantinedTooltip} />
    : null;

const failedPartitionsLink = (observer: ObserverRow, eventStore: string, namespace: string) => {
    if (observer.failedPartitionCount === 0) {
        return null;
    }

    // A real link, so middle-click, modifier-click and copy-link-address all behave. A div with an
    // onClick looks the same and is none of those things.
    return (
        <Link
            className='underline'
            to={`/event-store/${eventStore}/${namespace}/failed-partitions?observerId=${encodeURIComponent(observer.id)}`}>
            {observer.failedPartitionCount}
        </Link>
    );
};

const enumFilterOptions = <TEnum extends number>(values: TEnum[], toText: (value: TEnum) => string) =>
    values.map(value => ({ label: toText(value), value }));

const observerTypeFilterOptions = enumFilterOptions(
    [ObserverType.unknown, ObserverType.reactor, ObserverType.projection, ObserverType.reducer, ObserverType.external],
    getObserverTypeAsText);

const observerOwnerFilterOptions = enumFilterOptions(
    [ObserverOwner.none, ObserverOwner.client, ObserverOwner.kernel],
    getObserverOwnerAsText);

const runningStateFilterOptions = enumFilterOptions(
    [
        ObserverRunningState.unknown,
        ObserverRunningState.active,
        ObserverRunningState.suspended,
        ObserverRunningState.replaying,
        ObserverRunningState.disconnected,
        ObserverRunningState.quarantined
    ],
    getObserverRunningStateAsText);

const sequenceTypeFilterOptions = [
    { label: strings.eventStore.namespaces.observers.sequenceTypes.eventLog, value: ObserverSequenceType.eventLog },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.system, value: ObserverSequenceType.system },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.outbox, value: ObserverSequenceType.outbox },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.inbox, value: ObserverSequenceType.inbox },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.custom, value: ObserverSequenceType.custom }
];

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [searchText, setSearchText] = useState('');

    const queryArgs: AllObserversParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    const [observers] = AllObservers.when(!!params.namespace).use(queryArgs);
    const [failedPartitions] = AllFailedPartitions.when(!!params.namespace).use(queryArgs);

    // Counted here rather than carried on ObserverInformation, which is a hand-written gRPC contract
    // this change deliberately leaves alone. The trade is a namespace-wide subscription in the browser
    // for a per-row number; see #4140 for the server-side alternative.
    const failedPartitionCounts = (failedPartitions.data ?? []).reduce((counts, failedPartition) => {
        counts.set(failedPartition.observerId, (counts.get(failedPartition.observerId) ?? 0) + 1);
        return counts;
    }, new Map<string, number>());

    const searchTerm = searchText.trim().toLowerCase();

    const observerRows = (observers.data ?? [])
        .map(observer => ({
            ...observer,
            sequenceType: getObserverSequenceType(observer.eventSequenceId),
            isQuarantined: observer.runningState === ObserverRunningState.quarantined,
            failedPartitionCount: failedPartitionCounts.get(observer.id) ?? 0,
        }))
        .filter(observer => searchTerm === ''
            || observer.id.toLowerCase().includes(searchTerm)
            || observer.eventSequenceId.toLowerCase().includes(searchTerm));

    const menuItems: ActionMenuItem[] = [
        {
            label: strings.eventStore.namespaces.observers.actions.replay,
            icon: <faIcons.FaArrowsRotate className='mr-2' />,
            disabled: !viewModel.canReplay,
            command: () => viewModel.replay(params.eventStore!, params.namespace!)
        },
        {
            label: strings.eventStore.namespaces.observers.actions.clearQuarantine,
            icon: <faIcons.FaShield className='mr-2' />,
            disabled: !viewModel.canClearObserverQuarantine,
            command: () => viewModel.clearObserverQuarantine(params.eventStore!, params.namespace!)
        },
        {
            label: strings.eventStore.namespaces.observers.actions.remove,
            icon: <faIcons.FaTrash className='mr-2' />,
            disabled: !viewModel.canRemoveObserver,
            command: () => viewModel.removeObserver(params.eventStore!, params.namespace!)
        }
    ];

    const searchInput = (
        <IconField.Root>
            <IconField.Inset>
                <i className='pi pi-search' />
            </IconField.Inset>
            <InputText
                value={searchText}
                onChange={(event: ChangeEvent<HTMLInputElement>) => setSearchText(event.target.value)}
                placeholder={strings.eventStore.namespaces.observers.search}
            />
        </IconField.Root>
    );

    return (
        <Page title={strings.eventStore.namespaces.observers.title}>
            <div className='px-4 py-2 flex items-center justify-between gap-2'>
                <ActionMenubar model={menuItems} />
                {searchInput}
            </div>
            <div className='flex-1 overflow-hidden px-4 pb-4'>
                <Allotment className='h-full' proportionalLayout={false}>
                    <Allotment.Pane className='flex-grow'>
                        <DataTableCore<ObserverInformation>
                            data={observerRows}
                            selectionMode='single'
                            selection={viewModel.selectedObserver}
                            onSelectionChange={(event) => (viewModel.selectedObserver = event.value ?? undefined)}
                            dataKey='id'
                            emptyMessage={strings.eventStore.namespaces.observers.empty}
                            scrollable
                            scrollHeight='flex'
                            style={{ height: '100%' }}>
                            <Column
                                field='isQuarantined'
                                header={strings.eventStore.namespaces.observers.columns.quarantined}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                dataType='boolean'
                                filterField='isQuarantined'
                                body={quarantined} />
                            <Column
                                field='failedPartitionCount'
                                header={strings.eventStore.namespaces.observers.columns.failedPartitions}
                                sortable
                                dataType='numeric'
                                body={(observer: ObserverRow) => failedPartitionsLink(observer, params.eventStore!, params.namespace!)} />
                            <Column field='id' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                            <Column
                                field='eventSequenceId'
                                header={strings.eventStore.namespaces.observers.columns.sequence}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='sequenceType'
                                filterOptions={sequenceTypeFilterOptions} />
                            <Column
                                field='type'
                                header={strings.eventStore.namespaces.observers.columns.observerType}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='type'
                                filterOptions={observerTypeFilterOptions}
                                body={observerType} />
                            <Column
                                field='owner'
                                header={strings.eventStore.namespaces.observers.columns.owner}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='owner'
                                filterOptions={observerOwnerFilterOptions}
                                body={observerOwner} />
                            <Column
                                field='nextEventSequenceNumber'
                                dataType='numeric'
                                header={strings.eventStore.namespaces.observers.columns.nextEventSequenceNumber}
                                sortable />
                            <Column
                                field='handledEventCount'
                                dataType='numeric'
                                header={strings.eventStore.namespaces.observers.columns.handledEventCount}
                                sortable />
                            <Column
                                field='runningState'
                                header={strings.eventStore.namespaces.observers.columns.state}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='runningState'
                                filterOptions={runningStateFilterOptions}
                                body={runningState} />
                        </DataTableCore>
                    </Allotment.Pane>
                    {viewModel.selectedObserver &&
                        <Allotment.Pane preferredSize='450px'>
                            <ObserverDetails
                                observer={viewModel.selectedObserver}
                                eventStore={params.eventStore!}
                                namespace={params.namespace!} />
                        </Allotment.Pane>}
                </Allotment>
            </div>
        </Page>
    );
});
