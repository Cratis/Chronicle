// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from 'MVVM';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { ObserverInformation } from 'API/events/store/observers/ObserverInformation';
import { ObserverRunningState } from 'API/events/store/observers/ObserverRunningState';
import { ObserverType } from 'API/events/store/observers/ObserverType';

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
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'> Observers</h1>

            <DataTable value={viewModel.observers}>
                <Column field="observerId" header="Id" sortable />
                <Column field="name" header="Name" sortable />
                <Column field="type" header="ObserverType" sortable body={observerType} />
                <Column field="nextEventSequenceNumber" header="Next event sequence number" sortable />
                <Column field="runningState" header="State" sortable body={observerRunningState} />
            </DataTable>
        </div>
    )
});
