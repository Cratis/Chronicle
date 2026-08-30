// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ChangeEvent, useState } from 'react';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { ObserversViewModel } from './ObserversViewModel';
import { Column } from '@cratis/components/DataTables';
import { DataTable } from 'Components/DataTable';
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

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [searchText, setSearchText] = useState('');

    const queryArgs: AllObserversParameters = {
        eventStore: params.eventStore!,
        namespace: viewModel.currentNamespace
    };

    const [observers] = AllObservers.when(!!viewModel.currentNamespace).use(queryArgs);

    const searchTerm = searchText.trim().toLowerCase();

    const observerRows = (observers.data ?? [])
        .map(observer => ({
            ...observer,
            sequenceType: getObserverSequenceType(observer.eventSequenceId),
        }))
        .filter(observer => searchTerm === ''
            || observer.id.toLowerCase().includes(searchTerm)
            || observer.eventSequenceId.toLowerCase().includes(searchTerm));

    const menuItems: ActionMenuItem[] = [
        {
            label: strings.eventStore.namespaces.observers.actions.replay,
            icon: <faIcons.FaArrowsRotate className='mr-2' />,
            disabled: !viewModel.selectedObserver,
            command: () => viewModel.replay()
        },
        {
            label: strings.eventStore.namespaces.observers.actions.clearQuarantine,
            icon: <faIcons.FaShield className='mr-2' />,
            disabled: !viewModel.canClearObserverQuarantine,
            command: () => viewModel.clearObserverQuarantine()
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
                        <DataTable<ObserverInformation>
                            value={observerRows}
                            selectionMode='single'
                            selection={viewModel.selectedObserver}
                            onSelectionChange={(event) => (viewModel.selectedObserver = event.value ?? undefined)}
                            dataKey='id'
                            emptyMessage={strings.eventStore.namespaces.observers.empty}
                            scrollable
                            scrollHeight='flex'
                            style={{ height: '100%' }}>
                            <Column field='id' header={strings.eventStore.namespaces.observers.columns.id} sortable />
                            <Column
                                field='eventSequenceId'
                                header={strings.eventStore.namespaces.observers.columns.sequence}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='sequenceType' />
                            <Column
                                field='type'
                                header={strings.eventStore.namespaces.observers.columns.observerType}
                                sortable
                                dataType='numeric'
                                showFilterMatchModes={false}
                                filter
                                filterField='type'
                                body={observerType} />
                            <Column
                                field='owner'
                                header={strings.eventStore.namespaces.observers.columns.owner}
                                sortable
                                dataType='numeric'
                                showFilterMatchModes={false}
                                filter
                                filterField='owner'
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
                                dataType='numeric'
                                header={strings.eventStore.namespaces.observers.columns.state}
                                sortable
                                showFilterMatchModes={false}
                                filter
                                filterField='runningState'
                                body={runningState} />
                        </DataTable>
                    </Allotment.Pane>
                    {viewModel.selectedObserver &&
                        <Allotment.Pane preferredSize='450px'>
                            <ObserverDetails
                                observer={viewModel.selectedObserver}
                                eventStore={params.eventStore!}
                                namespace={viewModel.currentNamespace} />
                        </Allotment.Pane>}
                </Allotment>
            </div>
        </Page>
    );
});
