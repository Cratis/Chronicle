// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';

/**
 * The editable state of one event sequence query.
 *
 * This is the shape the editor works on and the shape that gets persisted; it deliberately mirrors
 * the saved query so that saving is a straight projection and never needs reconciling.
 */
export interface SequenceQueryState {
    /** The unique identifier of the query. */
    id: string;
    /** The display name shown on the query's tab. */
    name: string;
    /** Who the query is visible to. */
    scope: SequenceQueryScope;
    /** The namespace the query runs against. */
    namespace: string;
    /** The event sequence the query runs against. */
    eventSequenceId: string;
    /** The event source to narrow to, or empty for every event source. */
    eventSourceId: string;
    /** The event type identifiers to narrow to, or empty for every event type. */
    eventTypes: string[];
    /** The tags to narrow to, or empty for every event. */
    tags: string[];
    /** The inclusive lower bound on when the event occurred, in epoch milliseconds. */
    occurredFrom?: number;
    /** The exclusive upper bound on when the event occurred, in epoch milliseconds. */
    occurredTo?: number;
    /** Whether results are ordered newest first. */
    descending: boolean;
}

/** The event sequence a newly created query starts on. */
export const defaultEventSequenceId = 'event-log';

/**
 * Create the state for a brand new query.
 * @param id The identifier to give the query.
 * @param name The display name to give the query.
 * @param namespace The namespace the query runs against.
 * @returns The new query state.
 */
export const createSequenceQueryState = (id: string, name: string, namespace: string): SequenceQueryState => ({
    id,
    name,
    scope: SequenceQueryScope.user,
    namespace,
    eventSequenceId: defaultEventSequenceId,
    eventSourceId: '',
    eventTypes: [],
    tags: [],
    descending: true
});

/**
 * Convert a saved query into editable state.
 * @param query The saved query.
 * @returns The editable state.
 */
export const toSequenceQueryState = (query: SequenceQuery): SequenceQueryState => ({
    id: query.id,
    name: query.name,
    scope: query.scope,
    namespace: query.namespace,
    eventSequenceId: query.eventSequenceId,
    eventSourceId: query.eventSourceId ?? '',
    eventTypes: [...(query.eventTypes ?? [])],
    tags: [...(query.tags ?? [])],
    occurredFrom: query.occurredFrom ? new Date(query.occurredFrom).getTime() : undefined,
    occurredTo: query.occurredTo ? new Date(query.occurredTo).getTime() : undefined,
    descending: query.descending
});

/**
 * Determine whether two query states differ in any way that should be persisted.
 *
 * Saving happens as the user edits rather than behind a save button, so this is what keeps an
 * unchanged query from being written back on every render.
 * @param first The first state.
 * @param second The second state.
 * @returns True when the two states are equivalent.
 */
export const areSequenceQueryStatesEqual = (first: SequenceQueryState, second: SequenceQueryState): boolean =>
    first.id === second.id &&
    first.name === second.name &&
    first.scope === second.scope &&
    first.namespace === second.namespace &&
    first.eventSequenceId === second.eventSequenceId &&
    first.eventSourceId === second.eventSourceId &&
    first.descending === second.descending &&
    first.occurredFrom === second.occurredFrom &&
    first.occurredTo === second.occurredTo &&
    areSetsEqual(first.eventTypes, second.eventTypes) &&
    areSetsEqual(first.tags, second.tags);

/**
 * Determine whether a query narrows on anything at all.
 * @param state The query state.
 * @returns True when at least one filter is set.
 */
export const hasAnyFilter = (state: SequenceQueryState): boolean =>
    state.eventSourceId.trim().length > 0 ||
    state.eventTypes.length > 0 ||
    state.tags.length > 0 ||
    state.occurredFrom !== undefined ||
    state.occurredTo !== undefined;

/**
 * Clear every filter on a query, leaving its identity, sequence and ordering intact.
 * @param state The query state.
 * @returns The state with no narrowing.
 */
export const withoutFilters = (state: SequenceQueryState): SequenceQueryState => ({
    ...state,
    eventSourceId: '',
    eventTypes: [],
    tags: [],
    occurredFrom: undefined,
    occurredTo: undefined
});

const areSetsEqual = (first: string[], second: string[]): boolean => {
    if (first.length !== second.length) return false;

    const sortedSecond = [...second].sort();
    return [...first].sort().every((value, index) => value === sortedSecond[index]);
};
