// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column, DataTableCore } from '@cratis/components/DataTables';
import { ObserverInformation } from 'Features/Observation';
import strings from 'Strings';
import css from './ObserverEventTypes.module.css';

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
        <div className={css.observerEventTypes}>
            <DataTableCore
                data={observer.eventTypes ?? []}
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
            </DataTableCore>
        </div>
    );
};
