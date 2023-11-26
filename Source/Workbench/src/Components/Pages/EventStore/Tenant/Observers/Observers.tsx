// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from 'MVVM';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { ObserverInformation } from 'API/events/store/observers/ObserverInformation';
import { ObserverRunningState } from 'API/events/store/observers/ObserverRunningState';
import { ObserverType } from 'API/events/store/observers/ObserverType';
import { Filters } from '../../../../Filters/Filters/Filters';
import { Page } from '../../../Page';
import { ObserverState } from 'API/events/store/observers/ObserverState';
import { Toolbar } from 'primereact/toolbar';
import { Button } from 'primereact/button';
import * as mdIcons from 'react-icons/md';

const observerType = (observer: ObserverInformation) => {
    switch (observer.type) {
        case ObserverType.unknown: return 'Unknown';
        case ObserverType.client: return 'Client';
        case ObserverType.projection: return 'Projection';
        case ObserverType.inbox: return 'Inbox';
        case ObserverType.reducer: return 'Reducer';
    }
    return '[N/A]';
}

const observerRunningState = (observer: ObserverInformation) => {
    switch (observer.runningState) {
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

export const Observers = withViewModel(ObserversViewModel, ({ viewModel }) => {

    const toolbar = () => {
        return (
            <>
                {viewModel.selectedObserver &&
                    <Button label="Replay" icon={mdIcons.MdReplay} className="mr-2" />
                }
            </>
        )
    }

    return (
        <Page title='Observers'>
            <Toolbar aria-label='Actions' start={toolbar} />

            <Filters />

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
            >
                <Column field="observerId" header="Id" sortable />
                <Column field="name" header="Name" sortable />
                <Column field="type" header="ObserverType" sortable body={observerType} />
                <Column field="nextEventSequenceNumber" header="Next event sequence number" sortable />
                <Column field="runningState" header="State" sortable body={observerRunningState} />
            </DataTable>
        </Page>
    )
});
