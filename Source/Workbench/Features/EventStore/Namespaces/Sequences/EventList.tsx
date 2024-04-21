// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';

export interface EventListProps {
    events: any[];
}

export const EventList = (props: EventListProps) => {

    return (
        <>
            <DataTable
                rows={100}
                paginator
                scrollable
                scrollHeight={'flex'}
                selectionMode='single'
                filterDisplay='menu'
                emptyMessage='No events'
                dataKey='id'
                value={props.events}>
                <Column field='sequenceId' header='Id' sortable />
                <Column field='name' header='Name' sortable />
                <Column field='type' header='Sequence' sortable />
                <Column
                    field='nextEventSequenceNumber'
                    dataType='numeric'
                    header='Next event sequence number'
                    sortable
                />
                <Column field='runningState' header='State' sortable />
            </DataTable>
        </>
    );
};
