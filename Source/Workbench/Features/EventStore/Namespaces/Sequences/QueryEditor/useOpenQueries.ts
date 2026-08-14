// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef, useState } from 'react';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { OpenQuery, asSaved } from './OpenQuery';
import { SequenceQueryState, createSequenceQueryState, toSequenceQueryState } from './SequenceQueryState';
import { RememberedQueries, readRemembered, stillOpenable, writeRemembered } from './rememberedQueries';

/**
 * Decide which queries should be open, given the saved ones and the ones already open.
 *
 * The saved list only ever seeds the initial set: once something is open it keeps its in-flight
 * edits, so a save landing back from the server cannot reset what the user is typing. What was open
 * last time is picked up again where it still exists; failing that the first saved query opens, and
 * failing that a new one, so the page is never empty.
 * @param saved The saved queries visible to the user.
 * @param open The queries currently open.
 * @param newQueryName The name to give a query created because there were none.
 * @param newQueryId The identifier to give that query.
 * @param namespace The namespace new queries run against.
 * @param remembered What was open the last time the workspace was used.
 * @returns The queries that should be open, and which of them to show.
 */
export const reconcileOpenQueries = (
    saved: SequenceQuery[],
    open: OpenQuery[],
    newQueryName: string,
    newQueryId: string,
    namespace: string,
    remembered: RememberedQueries | null = null
): { open: OpenQuery[]; activeIndex: number } => {
    if (open.length > 0) return { open, activeIndex: -1 };

    const reopened = remembered ? stillOpenable(remembered, saved) : { queries: [], activeIndex: -1 };
    if (reopened.queries.length > 0) {
        return { open: reopened.queries.map(query => asSaved(toSequenceQueryState(query))), activeIndex: reopened.activeIndex };
    }

    if (saved.length === 0) {
        return { open: [{ state: createSequenceQueryState(newQueryId, newQueryName, namespace), saved: null }], activeIndex: 0 };
    }

    return { open: [asSaved(toSequenceQueryState(saved[0]))], activeIndex: 0 };
};

/**
 * Track the queries the user has open, seeded from what they had open last time.
 * @param saved The saved queries visible to the user.
 * @param namespace The namespace new queries run against.
 * @param newQueryName The name to give a newly created query.
 * @param isReady Whether the saved queries have been read yet.
 * @returns The open queries and the operations that change them.
 */
export const useOpenQueries = (saved: SequenceQuery[], namespace: string, newQueryName: string, isReady: boolean) => {
    const [open, setOpen] = useState<OpenQuery[]>([]);
    const [activeIndex, setActiveIndex] = useState(0);

    // Nothing opens until the saved queries have arrived: seeding against an empty list would find
    // none of the remembered queries and replace the whole workspace with a blank tab.
    useEffect(() => {
        if (!isReady) return;

        setOpen(current => {
            const reconciled = reconcileOpenQueries(
                saved, current, newQueryName, crypto.randomUUID(), namespace, readRemembered(namespace));
            if (reconciled.activeIndex >= 0) setActiveIndex(Math.max(0, reconciled.activeIndex));

            return reconciled.open;
        });
    }, [saved, namespace, newQueryName, isReady]);

    // Only queries that exist on the server can be picked up again, so an unsaved tab is left out
    // rather than remembered as something that cannot be restored.
    const hasReconciled = useRef(false);
    useEffect(() => {
        if (open.length === 0) return;
        if (!hasReconciled.current) {
            hasReconciled.current = true;
            return;
        }

        writeRemembered(namespace, {
            ids: open.filter(query => query.saved !== null).map(query => query.state.id),
            activeIndex
        });
    }, [open, activeIndex, namespace]);

    const update = useCallback((index: number, state: SequenceQueryState) => {
        setOpen(current => current.map((query, position) => (position === index ? { ...query, state } : query)));
    }, []);

    const markSaved = useCallback((state: SequenceQueryState) => {
        setOpen(current => current.map(query => (query.state.id === state.id ? asSaved(state) : query)));
    }, []);

    // A rename or a move made from the hierarchy is already written back, so both the state being
    // edited and the baseline it is compared against move together - what was unsaved stays unsaved.
    const applyPersisted = useCallback((change: (state: SequenceQueryState) => SequenceQueryState) => {
        setOpen(current => current.map(query => ({
            state: change(query.state),
            saved: query.saved && change(query.saved)
        })));
    }, []);

    const openSaved = useCallback((query: SequenceQuery) => {
        setOpen(current => {
            const existing = current.findIndex(candidate => candidate.state.id === query.id);
            if (existing >= 0) {
                setActiveIndex(existing);
                return current;
            }

            setActiveIndex(current.length);
            return [...current, asSaved(toSequenceQueryState(query))];
        });
    }, []);

    const add = useCallback((scope: SequenceQueryScope = SequenceQueryScope.user, folder = '') => {
        setOpen(current => {
            setActiveIndex(current.length);
            const state = createSequenceQueryState(crypto.randomUUID(), newQueryName, namespace, scope, folder);

            return [...current, { state, saved: null }];
        });
    }, [namespace, newQueryName]);

    const closeAt = useCallback((index: number) => {
        setOpen(current => {
            if (index < 0 || index >= current.length) return current;

            const remaining = current.filter((_, position) => position !== index);
            setActiveIndex(previous => Math.max(0, Math.min(previous, remaining.length - 1)));

            if (remaining.length > 0) return remaining;

            return [{ state: createSequenceQueryState(crypto.randomUUID(), newQueryName, namespace), saved: null }];
        });
    }, [namespace, newQueryName]);

    const closeById = useCallback((id: string) =>
        setOpen(current => {
            const index = current.findIndex(candidate => candidate.state.id === id);
            if (index < 0) return current;

            const remaining = current.filter((_, position) => position !== index);
            setActiveIndex(previous => Math.max(0, Math.min(previous, remaining.length - 1)));

            if (remaining.length > 0) return remaining;

            return [{ state: createSequenceQueryState(crypto.randomUUID(), newQueryName, namespace), saved: null }];
        }), [namespace, newQueryName]);

    return { open, activeIndex, setActiveIndex, update, markSaved, applyPersisted, add, close: closeAt, closeById, openSaved };
};
