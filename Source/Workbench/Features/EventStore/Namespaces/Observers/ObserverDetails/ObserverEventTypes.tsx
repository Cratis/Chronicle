// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { ObserverInformation } from 'Api/Observation';
import strings from 'Strings';
import './ObserverEventTypes.css';

/**
 * Props for {@link ObserverEventTypes}.
 */
export interface ObserverEventTypesProps {
    /**
     * The observer to list event types for.
     */
    observer: ObserverInformation;
}

/**
 * Renders the list of event types consumed by the observer.
 *
 * @param props - The {@link ObserverEventTypesProps}.
 */
export const ObserverEventTypes = ({ observer }: ObserverEventTypesProps) => {
    const eventTypesStrings = strings.eventStore.namespaces.observers.details.eventTypes;

    return (
        <div className='observer-event-types'>
            <DataTable
                value={observer.eventTypes ?? []}
                dataKey='id'
                emptyMessage={eventTypesStrings.empty}
                scrollable
                scrollHeight='flex'
                style={{ height: '100%' }}>
                <Column
                    field='id'
                    header={eventTypesStrings.columns.id}
                    sortable />
                <Column
                    field='generation'
                    header={eventTypesStrings.columns.generation}
                    sortable />
            </DataTable>
        </div>
    );
};
