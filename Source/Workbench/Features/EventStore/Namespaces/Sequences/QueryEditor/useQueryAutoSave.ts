// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { SaveSequenceQuery } from 'Api/SequenceQueries/SaveSequenceQuery';
import { SequenceQueryState, areSequenceQueryStatesEqual } from './SequenceQueryState';

/** How long editing has to settle before the query is written back. */
export const autoSaveDelayInMilliseconds = 600;

/**
 * Persist a query as the user edits it, rather than behind a save button.
 *
 * Edits are coalesced so that dragging a range handle or typing an event source id results in one
 * write once the user settles, not one per keystroke. The last saved state is remembered so an
 * unchanged query - a re-render, or reopening a saved tab - never writes at all.
 * @param state The current query state.
 * @param eventStore The event store the query belongs to.
 * @param isEnabled Whether saving should happen at all.
 */
export const useQueryAutoSave = (state: SequenceQueryState, eventStore: string, isEnabled: boolean) => {
    const lastSaved = useRef<SequenceQueryState | undefined>(undefined);

    useEffect(() => {
        if (!isEnabled) return;
        if (lastSaved.current && areSequenceQueryStatesEqual(lastSaved.current, state)) return;

        const handle = setTimeout(async () => {
            const command = new SaveSequenceQuery();
            command.eventStore = eventStore;
            command.id = state.id;
            command.name = state.name;
            command.scope = state.scope;
            command.namespace = state.namespace;
            command.eventSequenceId = state.eventSequenceId;
            command.eventSourceId = state.eventSourceId;
            command.eventTypes = state.eventTypes;
            command.tags = state.tags;
            command.occurredFrom = state.occurredFrom !== undefined ? new Date(state.occurredFrom) : undefined;
            command.occurredTo = state.occurredTo !== undefined ? new Date(state.occurredTo) : undefined;
            command.descending = state.descending;

            const result = await command.execute();
            if (result.isSuccess) {
                lastSaved.current = state;
            }
        }, autoSaveDelayInMilliseconds);

        return () => clearTimeout(handle);
    }, [state, eventStore, isEnabled]);
};
