// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { ObserverInformationForEventType } from 'Api/Observation';
import { getObserverRunningStateAsText } from '../../../Namespaces/Observers/getObserverRunningStateAsText';
import { renderObserverType } from '../../../Namespaces/Observers/ObserverDetails';
import strings from 'Strings';
import './ObserversForEventTypeTable.css';

/**
 * Props for {@link ObserversForEventTypeTable}.
 */
export interface ObserversForEventTypeTableProps {
    /**
     * The observers consuming the event type to render.
     */
    observers: readonly ObserverInformationForEventType[];
}

const renderRowObserverType = (row: ObserverInformationForEventType): string =>
    renderObserverType(row.observer);

const renderRowRunningState = (row: ObserverInformationForEventType): string =>
    getObserverRunningStateAsText(row.observer.runningState);

/**
 * Renders the observers-consuming-event-type table.
 *
 * @param props - The {@link ObserversForEventTypeTableProps}.
 */
export const ObserversForEventTypeTable = ({ observers }: ObserversForEventTypeTableProps) => {
    const columnStrings = strings.eventStore.general.eventTypes.observers.columns;

    return (
        <DataTable
            value={observers as ObserverInformationForEventType[]}
            dataKey='observer.id'
            emptyMessage={strings.eventStore.general.eventTypes.observers.empty}
            scrollable
            scrollHeight='flex'
            className='observers-for-event-type-table'>
            <Column field='namespace' header={columnStrings.namespace} sortable />
            <Column field='observer.id' header={columnStrings.id} sortable />
            <Column
                field='observer.type'
                header={columnStrings.observerType}
                body={renderRowObserverType}
                sortable />
            <Column
                field='observer.runningState'
                header={columnStrings.state}
                body={renderRowRunningState}
                sortable />
        </DataTable>
    );
};
