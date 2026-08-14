// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';

const storageKey = 'cratis.workbench.sequences.openQueries';

/**
 * What was open the last time the workspace was used.
 */
export interface RememberedQueries {
    /** The identifiers of the saved queries that were open, in tab order. */
    ids: string[];
    /** Which of them was being looked at. */
    activeIndex: number;
}

/**
 * Work out which of the remembered queries can still be opened.
 *
 * A query that has been deleted since - by this user or by somebody else, if it was shared - simply
 * drops out, and the active tab moves to whatever is left in its place.
 * @param remembered What was open last time.
 * @param saved The saved queries visible to the user now.
 * @returns The queries to reopen, in tab order, and which of them to show.
 */
export const stillOpenable = (
    remembered: RememberedQueries,
    saved: SequenceQuery[]): { queries: SequenceQuery[]; activeIndex: number } => {
    const byId = new Map(saved.map(query => [query.id, query]));
    const queries = remembered.ids.map(id => byId.get(id)).filter((query): query is SequenceQuery => query !== undefined);

    // The remembered index counts tabs that may no longer exist, so it moves to where the query it
    // pointed at ended up - or to the nearest tab that survived.
    const activeId = remembered.ids[remembered.activeIndex];
    const activeIndex = queries.findIndex(query => query.id === activeId);

    return { queries, activeIndex: activeIndex >= 0 ? activeIndex : Math.min(remembered.activeIndex, queries.length - 1) };
};

/**
 * Read what was open the last time the workspace was used.
 * @param namespace The namespace the queries belong to.
 * @returns What was open, or nothing when this is the first visit.
 */
export const readRemembered = (namespace: string): RememberedQueries | null => {
    try {
        const stored = localStorage.getItem(keyFor(namespace));
        return stored ? JSON.parse(stored) as RememberedQueries : null;
    } catch {
        // A hand-edited or truncated value should start the user afresh rather than break the page.
        return null;
    }
};

/**
 * Remember what is open, so the next visit picks up where this one left off.
 * @param namespace The namespace the queries belong to.
 * @param remembered What is open.
 */
export const writeRemembered = (namespace: string, remembered: RememberedQueries) =>
    localStorage.setItem(keyFor(namespace), JSON.stringify(remembered));

const keyFor = (namespace: string) => `${storageKey}.${namespace}`;
