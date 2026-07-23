// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventType } from 'Api/Events';

/**
 * Builds the distinct, alphabetically sorted set of event-type filter options from the event types
 * registered in the event store, collapsing the multiple generations that share a single type id.
 * @param eventTypes The event types registered in the event store.
 * @returns Distinct dropdown options keyed by event type id.
 */
export const getDistinctEventTypeOptions = (eventTypes: EventType[]): { label: string; value: string }[] => {
    const seen = new Set<string>();
    const options: { label: string; value: string }[] = [];

    for (const eventType of eventTypes ?? []) {
        if (!seen.has(eventType.id)) {
            seen.add(eventType.id);
            options.push({ label: eventType.id, value: eventType.id });
        }
    }

    return options.sort((first, second) => first.label.localeCompare(second.label));
};
