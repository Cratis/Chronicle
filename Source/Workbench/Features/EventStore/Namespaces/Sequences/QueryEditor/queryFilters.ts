// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { FilterDefinition, FilterValues } from '@cratis/components/Filter';
import { SequenceQueryState } from './SequenceQueryState';

/** The filter group holding the event types to narrow to. */
export const eventTypeFilterKey = 'eventType';

/** The filter group holding the event source to narrow to. */
export const eventSourceFilterKey = 'eventSource';

/** The filter group holding the time range to narrow to. */
export const occurredFilterKey = 'occurred';

/**
 * Build the filter groups shown in the query's filter dropdown.
 *
 * The event source and time range are rendered by custom editors, because neither is a choice from
 * a fixed set of options: one is free text and the other is a histogram the server counts.
 * @param eventTypeIds The event type identifiers registered in the event store.
 * @param labels The user-facing labels for each group.
 * @returns The filter definitions for the panel.
 */
export const buildFilterDefinitions = (
    eventTypeIds: string[],
    labels: { eventType: string; eventSource: string; occurred: string; searchEventTypes: string }
): FilterDefinition[] => [
    {
        key: eventTypeFilterKey,
        label: labels.eventType,
        type: 'string',
        multi: true,
        searchable: true,
        searchPlaceholder: labels.searchEventTypes,
        options: eventTypeIds.map(id => ({ key: id, label: id, value: id }))
    },
    {
        key: eventSourceFilterKey,
        label: labels.eventSource,
        type: 'custom'
    },
    {
        key: occurredFilterKey,
        label: labels.occurred,
        type: 'custom'
    }
];

/**
 * Project the event types a query narrows to onto the panel's selection map.
 * @param state The query state.
 * @returns The selections for the panel.
 */
export const toFilterValues = (state: SequenceQueryState): FilterValues => ({
    [eventTypeFilterKey]: new Set(state.eventTypes)
});

/**
 * Apply a toggled event type option to a query.
 * @param state The query state.
 * @param eventTypeId The event type that was toggled.
 * @returns The state with that event type added or removed.
 */
export const toggleEventType = (state: SequenceQueryState, eventTypeId: string): SequenceQueryState => ({
    ...state,
    eventTypes: state.eventTypes.includes(eventTypeId)
        ? state.eventTypes.filter(id => id !== eventTypeId)
        : [...state.eventTypes, eventTypeId]
});

/**
 * Clear one filter group on a query.
 * @param state The query state.
 * @param filterKey The group to clear.
 * @returns The state with that group no longer narrowing.
 */
export const clearFilter = (state: SequenceQueryState, filterKey: string): SequenceQueryState => {
    switch (filterKey) {
        case eventTypeFilterKey:
            return { ...state, eventTypes: [] };
        case eventSourceFilterKey:
            return { ...state, eventSourceId: '' };
        case occurredFilterKey:
            return { ...state, occurredFrom: undefined, occurredTo: undefined };
        default:
            return state;
    }
};

/**
 * Count how many things each filter group currently narrows on, for the badge on the filter button.
 * @param state The query state.
 * @returns The number of active narrowings across all groups.
 */
export const countActiveFilters = (state: SequenceQueryState): number => {
    let count = state.eventTypes.length;
    if (state.eventSourceId.trim().length > 0) count++;
    if (state.occurredFrom !== undefined || state.occurredTo !== undefined) count++;

    return count;
};
