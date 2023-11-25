// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from 'MVVM';
import { ObserversViewModel } from './ObserversViewModel';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';

export interface IObserversProps {
    something: string;
}

export const Observers = withViewModel<ObserversViewModel, IObserversProps>(ObserversViewModel, ({ viewModel, props }) => {
    return (
        <div className='p-4'>
            <h1 className='text-3xl m-3'> Observers</h1>

            <DataTable value={viewModel.observers}>
                <Column field="observerId" header="Id" sortable/>
                <Column field="name" header="Name" sortable/>
                <Column field="type" header="ObserverType" sortable/>
                <Column field="nextEventSequenceNumber" header="Next event sequence number" sortable/>
                <Column field="runningState" header="State" sortable/>
            </DataTable>
        </div>
    )
});
