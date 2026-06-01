// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useParams } from 'react-router-dom';
import { ObserveObserversForEventType } from 'Api/Observation';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ObserversForEventTypeTable } from './ObserversForEventTypeTable';

/**
 * Props for {@link ObserversForEventType}.
 */
export interface ObserversForEventTypeProps {
    /**
     * The identifier of the event type to list consuming observers for.
     */
    eventTypeId: string;
}

/**
 * Subscribes to the reactive query for observers consuming the given event type and renders them in a table.
 *
 * @param props - The {@link ObserversForEventTypeProps}.
 */
export const ObserversForEventType = ({ eventTypeId }: ObserversForEventTypeProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [observersQuery] = ObserveObserversForEventType.use({
        eventStore: params.eventStore!,
        eventTypeId
    });

    return (
        <ObserversForEventTypeTable observers={observersQuery.data ?? []} />
    );
};
