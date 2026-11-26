// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeVisibility } from 'Features/Contracts/Events';
import { EventTypeDetails } from 'Features/EventTypes';

const outboxSequenceId = 'outbox';

/**
 * Builds the distinct, alphabetically sorted set of event-type filter options from the event types
 * registered in the event store, collapsing the multiple generations that share a single type id.
 * The outbox only exposes public event types, so for that sequence only types currently registered as public are offered.
 * @param eventTypes The event types registered in the event store.
 * @param eventSequenceId The event sequence the options are for. Every type is offered when omitted.
 * @returns Distinct dropdown options keyed by event type id.
 */
export const getDistinctEventTypeOptions = (eventTypes: EventTypeDetails[], eventSequenceId?: string): { label: string; value: string }[] => {
    const seen = new Set<string>();
    const options: { label: string; value: string }[] = [];

    for (const eventType of eventTypes ?? []) {
        if (eventSequenceId === outboxSequenceId && eventType.visibility !== EventTypeVisibility.public) {
            continue;
        }

        if (!seen.has(eventType.type.id)) {
            seen.add(eventType.type.id);
            options.push({ label: eventType.type.id, value: eventType.type.id });
        }
    }

    return options.sort((first, second) => first.label.localeCompare(second.label));
};
