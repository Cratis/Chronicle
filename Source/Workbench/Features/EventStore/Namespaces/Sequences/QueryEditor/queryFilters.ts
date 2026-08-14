// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { FilterDefinition, FilterValues } from '@cratis/components/Filter';
import { SequenceQueryState } from './SequenceQueryState';

/** The filter group holding the event types to narrow to. */
export const eventTypeFilterKey = 'eventType';

/** The filter group holding the event source to narrow to. */
export const eventSourceFilterKey = 'eventSource';

/** The filter group holding the event source type to narrow to. */
export const eventSourceTypeFilterKey = 'eventSourceType';

/** The filter group holding the event stream type to narrow to. */
export const eventStreamTypeFilterKey = 'eventStreamType';

/** The filter group holding the correlation to narrow to. */
export const correlationFilterKey = 'correlation';

/** The filter group holding the tags to narrow to. */
export const tagsFilterKey = 'tags';

/** The filter group holding the time range to narrow to. */
export const occurredFilterKey = 'occurred';

/**
 * The labels each filter group is rendered with.
 */
export interface QueryFilterLabels {
    /** The label for the event type group. */
    eventType: string;
    /** The label for the event source group. */
    eventSource: string;
    /** The label for the event source type group. */
    eventSourceType: string;
    /** The label for the event stream type group. */
    eventStreamType: string;
    /** The label for the correlation group. */
    correlation: string;
    /** The label for the tags group. */
    tags: string;
    /** The label for the time range group. */
    occurred: string;
    /** The placeholder for the event type search box. */
    searchEventTypes: string;
}

/**
 * Build the filter groups shown in the query's filter dropdown.
 *
 * The groups mirror the dimensions the pivot viewer offers, so the two ways of looking at a
 * sequence narrow on the same things. Only the event type is a choice from a known set - the rest
 * are values the server matches on and are therefore rendered by custom editors rather than as
 * option lists, which would mean reading the whole sequence just to enumerate them.
 * @param eventTypeIds The event type identifiers registered in the event store.
 * @param labels The user-facing labels for each group.
 * @returns The filter definitions for the panel.
 */
export const buildFilterDefinitions = (
    eventTypeIds: string[],
    labels: QueryFilterLabels
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
    { key: eventSourceFilterKey, label: labels.eventSource, type: 'custom' },
    { key: eventSourceTypeFilterKey, label: labels.eventSourceType, type: 'custom' },
    { key: eventStreamTypeFilterKey, label: labels.eventStreamType, type: 'custom' },
    { key: correlationFilterKey, label: labels.correlation, type: 'custom' },
    { key: tagsFilterKey, label: labels.tags, type: 'custom' },
    { key: occurredFilterKey, label: labels.occurred, type: 'custom' }
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
 * Project the free-form narrowings onto the values the panel's custom editors render.
 *
 * A group with an undefined value shows no clear button, which is what tells the user at a glance
 * whether that dimension narrows anything.
 * @param state The query state.
 * @returns The custom editor values, keyed by filter group.
 */
export const toCustomFilterValues = (state: SequenceQueryState): Record<string, unknown> => ({
    [eventSourceFilterKey]: state.eventSourceId || undefined,
    [eventSourceTypeFilterKey]: state.eventSourceType || undefined,
    [eventStreamTypeFilterKey]: state.eventStreamType || undefined,
    [correlationFilterKey]: state.correlationId || undefined,
    [tagsFilterKey]: state.tags.length > 0 ? state.tags : undefined,
    [occurredFilterKey]: state.occurredFrom !== undefined && state.occurredTo !== undefined
        ? [state.occurredFrom, state.occurredTo]
        : undefined
});

/**
 * Apply a value a custom editor produced to a query.
 * @param state The query state.
 * @param filterKey The group the value belongs to.
 * @param value The value the editor produced, or undefined when it was cleared.
 * @returns The state narrowed by that value.
 */
export const applyCustomFilterValue = (
    state: SequenceQueryState,
    filterKey: string,
    value: unknown): SequenceQueryState => {
    switch (filterKey) {
        case eventSourceFilterKey:
            return { ...state, eventSourceId: (value as string) ?? '' };
        case eventSourceTypeFilterKey:
            return { ...state, eventSourceType: (value as string) ?? '' };
        case eventStreamTypeFilterKey:
            return { ...state, eventStreamType: (value as string) ?? '' };
        case correlationFilterKey:
            return { ...state, correlationId: (value as string) ?? '' };
        case tagsFilterKey:
            return { ...state, tags: (value as string[]) ?? [] };
        case occurredFilterKey: {
            const range = value as [number, number] | undefined;
            return { ...state, occurredFrom: range?.[0], occurredTo: range?.[1] };
        }
        default:
            return state;
    }
};

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
    if (filterKey === eventTypeFilterKey) return { ...state, eventTypes: [] };

    return applyCustomFilterValue(state, filterKey, undefined);
};

/**
 * Count how many things each filter group currently narrows on, for the badge on the filter button.
 * @param state The query state.
 * @returns The number of active narrowings across all groups.
 */
export const countActiveFilters = (state: SequenceQueryState): number => {
    const freeFormValues = [state.eventSourceId, state.eventSourceType, state.eventStreamType, state.correlationId];

    let count = state.eventTypes.length + state.tags.length;
    count += freeFormValues.filter(value => value.trim().length > 0).length;
    if (state.occurredFrom !== undefined || state.occurredTo !== undefined) count++;

    return count;
};
