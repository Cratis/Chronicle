// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeDetails } from 'Features/EventTypes';

/**
 * Builds the distinct, alphabetically sorted set of event-type filter options from the event types
 * registered in the event store, collapsing the multiple generations that share a single type id.
 * @param eventTypes The event types registered in the event store.
 * @returns Distinct dropdown options keyed by event type id.
 */
export const getDistinctEventTypeOptions = (eventTypes: EventTypeDetails[]): { label: string; value: string }[] => {
    const seen = new Set<string>();
    const options: { label: string; value: string }[] = [];

    for (const eventType of eventTypes ?? []) {
        if (!seen.has(eventType.type.id)) {
            seen.add(eventType.type.id);
            options.push({ label: eventType.type.id, value: eventType.type.id });
        }
    }

    return options.sort((first, second) => first.label.localeCompare(second.label));
};
