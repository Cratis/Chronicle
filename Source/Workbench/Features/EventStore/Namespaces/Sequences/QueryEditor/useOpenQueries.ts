// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useState } from 'react';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryState, createSequenceQueryState, toSequenceQueryState } from './SequenceQueryState';

/**
 * Decide which queries should be open, given the saved ones and the ones already open.
 *
 * The saved list only ever seeds the initial set: once something is open it keeps its in-flight
 * edits, so a save landing back from the server cannot reset what the user is typing. With nothing
 * saved yet the user still gets one new query, so the page is never empty.
 * @param saved The saved queries visible to the user.
 * @param open The queries currently open.
 * @param newQueryName The name to give a query created because there were none.
 * @param newQueryId The identifier to give that query.
 * @param namespace The namespace new queries run against.
 * @returns The queries that should be open.
 */
export const reconcileOpenQueries = (
    saved: SequenceQuery[],
    open: SequenceQueryState[],
    newQueryName: string,
    newQueryId: string,
    namespace: string
): SequenceQueryState[] => {
    if (open.length > 0) return open;
    if (saved.length === 0) return [createSequenceQueryState(newQueryId, newQueryName, namespace)];

    return [toSequenceQueryState(saved[0])];
};

/**
 * Track the queries the user has open, seeded from the queries they have saved.
 * @param saved The saved queries visible to the user.
 * @param namespace The namespace new queries run against.
 * @param newQueryName The name to give a newly created query.
 * @returns The open queries and the operations that change them.
 */
export const useOpenQueries = (saved: SequenceQuery[], namespace: string, newQueryName: string) => {
    const [open, setOpen] = useState<SequenceQueryState[]>([]);
    const [activeIndex, setActiveIndex] = useState(0);

    useEffect(() => {
        setOpen(current => reconcileOpenQueries(saved, current, newQueryName, crypto.randomUUID(), namespace));
    }, [saved, namespace, newQueryName]);

    const update = useCallback((index: number, state: SequenceQueryState) => {
        setOpen(current => current.map((query, position) => (position === index ? state : query)));
    }, []);

    const openSaved = useCallback((query: SequenceQuery) => {
        setOpen(current => {
            const existing = current.findIndex(open => open.id === query.id);
            if (existing >= 0) {
                setActiveIndex(existing);
                return current;
            }

            setActiveIndex(current.length);
            return [...current, toSequenceQueryState(query)];
        });
    }, []);

    const add = useCallback(() => {
        setOpen(current => {
            setActiveIndex(current.length);
            return [...current, createSequenceQueryState(crypto.randomUUID(), newQueryName, namespace)];
        });
    }, [namespace, newQueryName]);

    const close = useCallback((index: number) => {
        setOpen(current => {
            const remaining = current.filter((_, position) => position !== index);
            setActiveIndex(previous => Math.max(0, Math.min(previous, remaining.length - 1)));

            return remaining.length > 0
                ? remaining
                : [createSequenceQueryState(crypto.randomUUID(), newQueryName, namespace)];
        });
    }, [namespace, newQueryName]);

    return { open, activeIndex, setActiveIndex, update, add, close, openSaved };
};
