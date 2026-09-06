// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Features/SequenceQueries';
import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';

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
    /** The folder within the scope the query is filed under, or empty when it sits at the root. */
    folder: string;
    /** Whether the query has been saved at least once. */
    isSaved: boolean;
    /** The namespace the query runs against. */
    namespace: string;
    /** The event sequence the query runs against. */
    eventSequenceId: string;
    /** The event source to narrow to, or empty for every event source. */
    eventSourceId: string;
    /** The event source type to narrow to, or empty for every event source type. */
    eventSourceType: string;
    /** The event stream type to narrow to, or empty for every event stream type. */
    eventStreamType: string;
    /** The correlation to narrow to, or empty for every correlation. */
    correlationId: string;
    /** The event type identifiers to narrow to, or empty for every event type. */
    eventTypes: string[];
    /** The tags to narrow to, or empty for every event. */
    tags: string[];
    /** The inclusive lower bound on when the event occurred, in epoch milliseconds. */
    occurredFrom?: number;
    /** The exclusive upper bound on when the event occurred, in epoch milliseconds. */
    occurredTo?: number;
    /** What the results are ordered by. */
    sortBy: string;
    /** Whether results are ordered from the highest value down rather than from the lowest up. */
    descending: boolean;
}

/** The field a query orders by until the user picks another one. */
export const defaultSortBy = 'sequenceNumber';

/** The event sequence a newly created query starts on. */
export const defaultEventSequenceId = 'event-log';

/**
 * Create the state for a brand new query.
 * @param id The identifier to give the query.
 * @param name The display name to give the query.
 * @param namespace The namespace the query runs against.
 * @param scope Who the query should be visible to once it is saved.
 * @param folder The folder to file the query under once it is saved.
 * @returns The new query state.
 */
export const createSequenceQueryState = (
    id: string,
    name: string,
    namespace: string,
    scope: SequenceQueryScope = SequenceQueryScope.user,
    folder = ''): SequenceQueryState => ({
    id,
    name,
    scope,
    folder,
    isSaved: false,
    namespace,
    eventSequenceId: defaultEventSequenceId,
    eventSourceId: '',
    eventSourceType: '',
    eventStreamType: '',
    correlationId: '',
    eventTypes: [],
    tags: [],
    sortBy: defaultSortBy,
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
    folder: query.folder ?? '',
    isSaved: true,
    namespace: query.namespace,
    eventSequenceId: query.eventSequenceId,
    eventSourceId: query.eventSourceId ?? '',
    eventSourceType: query.eventSourceType ?? '',
    eventStreamType: query.eventStreamType ?? '',
    correlationId: query.correlationId ?? '',
    eventTypes: [...(query.eventTypes ?? [])],
    tags: [...(query.tags ?? [])],
    occurredFrom: query.occurredFrom ? new Date(query.occurredFrom).getTime() : undefined,
    occurredTo: query.occurredTo ? new Date(query.occurredTo).getTime() : undefined,
    sortBy: query.sortBy || defaultSortBy,
    descending: query.descending
});

/**
 * Determine whether two query states differ in any way that should be persisted.
 *
 * This is what tells the editor whether there is anything to save, which drives both the state of
 * the save action and the prompt before closing a tab with unsaved work.
 * @param first The first state.
 * @param second The second state.
 * @returns True when the two states are equivalent.
 */
export const areSequenceQueryStatesEqual = (first: SequenceQueryState, second: SequenceQueryState): boolean =>
    first.id === second.id &&
    first.name === second.name &&
    first.scope === second.scope &&
    first.folder === second.folder &&
    first.namespace === second.namespace &&
    first.eventSequenceId === second.eventSequenceId &&
    first.eventSourceId === second.eventSourceId &&
    first.eventSourceType === second.eventSourceType &&
    first.eventStreamType === second.eventStreamType &&
    first.correlationId === second.correlationId &&
    first.sortBy === second.sortBy &&
    first.descending === second.descending &&
    first.occurredFrom === second.occurredFrom &&
    first.occurredTo === second.occurredTo &&
    areSetsEqual(first.eventTypes, second.eventTypes) &&
    areSetsEqual(first.tags, second.tags);

const areSetsEqual = (first: string[], second: string[]): boolean => {
    if (first.length !== second.length) return false;

    const sortedSecond = [...second].sort();
    return [...first].sort().every((value, index) => value === sortedSecond[index]);
};
