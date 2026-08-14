// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQueryState, areSequenceQueryStatesEqual } from './SequenceQueryState';

/**
 * One query the user has open as a tab.
 */
export interface OpenQuery {
    /** The state the user is editing. */
    state: SequenceQueryState;

    /** The state as last written back, or null when the query has never been saved. */
    saved: SequenceQueryState | null;
}

/**
 * Determine whether an open query carries edits that have not been written back.
 *
 * A query that has never been saved always counts as changed, so the save action is offered from
 * the moment a new tab appears rather than only after something is typed into it.
 * @param query The open query.
 * @returns True when there is something to save.
 */
export const hasUnsavedChanges = (query: OpenQuery): boolean =>
    query.saved === null || !areSequenceQueryStatesEqual(query.saved, query.state);

/**
 * Take a copy of a state to remember as the last saved one.
 * @param state The state that was written back.
 * @returns The open query, with the state and its saved baseline in step.
 */
export const asSaved = (state: SequenceQueryState): OpenQuery => ({ state, saved: { ...state } });
