// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeDetails } from 'Features/EventTypes';

/**
 * The schema extension key an event type uses to say which event type it compensates for.
 */
const compensationForKey = 'compensationFor';

/**
 * Finds the event types that are declared as compensations for a given event type, keeping only the
 * latest generation of each.
 * @param eventTypes The event types registered in the event store.
 * @param compensatedEventTypeId The id of the event type being compensated for.
 * @returns The compensating event types, sorted alphabetically by id. Empty when none are registered.
 */
export const getCompensationEventTypes = (eventTypes: EventTypeDetails[], compensatedEventTypeId: string): EventTypeDetails[] => {
    const latest = new Map<string, EventTypeDetails>();

    for (const eventType of eventTypes ?? []) {
        if (!compensates(eventType, compensatedEventTypeId)) continue;

        const current = latest.get(eventType.type.id);
        if (!current || eventType.type.generation > current.type.generation) {
            latest.set(eventType.type.id, eventType);
        }
    }

    return [...latest.values()].sort((first, second) => first.type.id.localeCompare(second.type.id));
};

const compensates = (eventType: EventTypeDetails, compensatedEventTypeId: string): boolean => {
    try {
        const schema = JSON.parse(eventType.schema) as Record<string, unknown>;
        return schema[compensationForKey] === compensatedEventTypeId;
    } catch {
        return false;
    }
};
