// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SaveSequenceQuery } from 'Features/SequenceQueries';
import { SequenceQueryState } from './SequenceQueryState';

/**
 * Write a query back to the event store.
 *
 * The whole query is replaced every time rather than patched, which is what lets the editor treat
 * its own state as the single source of truth.
 * @param state The query state to persist.
 * @param eventStore The event store the query belongs to.
 * @returns True when the query was written, false when the command was rejected.
 */
export const saveSequenceQuery = async (state: SequenceQueryState, eventStore: string): Promise<boolean> => {
    const command = new SaveSequenceQuery();
    command.eventStore = eventStore;
    command.id = state.id;
    command.name = state.name;
    command.scope = state.scope;
    command.folder = state.folder;
    command.namespace = state.namespace;
    command.eventSequenceId = state.eventSequenceId;
    command.eventSourceId = state.eventSourceId;
    command.eventSourceType = state.eventSourceType;
    command.eventStreamType = state.eventStreamType;
    command.correlationId = state.correlationId;
    command.eventTypes = state.eventTypes;
    command.tags = state.tags;
    command.occurredFrom = state.occurredFrom !== undefined ? new Date(state.occurredFrom) : undefined;
    command.occurredTo = state.occurredTo !== undefined ? new Date(state.occurredTo) : undefined;
    command.sortBy = state.sortBy;
    command.descending = state.descending;

    const result = await command.execute();
    return result.isSuccess;
};
