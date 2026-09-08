// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryEventsParameters } from 'Features/Sequences';
import { SequenceHistogramParameters } from 'Features/Sequences';
import { SequenceQueryState } from './SequenceQueryState';

const narrowing = (state: SequenceQueryState) => ({
    eventSourceId: state.eventSourceId.trim() || undefined,
    eventSourceType: state.eventSourceType.trim() || undefined,
    eventStreamType: state.eventStreamType.trim() || undefined,
    correlationId: state.correlationId.trim() || undefined,
    eventTypeIds: state.eventTypes.length > 0 ? state.eventTypes.join(',') : undefined,
    tags: state.tags.length > 0 ? state.tags.join(',') : undefined
});

/**
 * Project a query's state onto the arguments its event query takes.
 *
 * Absent filters are sent as undefined rather than as empty strings, so the backend leaves those
 * dimensions unnarrowed instead of matching on an empty value. Ordering is deliberately absent:
 * Arc carries that on the query itself rather than as an argument.
 * @param state The query state.
 * @param eventStore The event store the query runs against.
 * @returns The arguments for the events query.
 */
export const toQueryArguments = (state: SequenceQueryState, eventStore: string): QueryEventsParameters => ({
    eventStore,
    namespace: state.namespace,
    eventSequenceId: state.eventSequenceId,
    ...narrowing(state),
    occurredFrom: state.occurredFrom !== undefined ? new Date(state.occurredFrom) : undefined,
    occurredTo: state.occurredTo !== undefined ? new Date(state.occurredTo) : undefined
});

/**
 * Project a query's state onto the arguments its histogram takes.
 *
 * The occurred bounds are deliberately left off: the histogram is what the user picks the range
 * *with*, so narrowing it by the current range would collapse it to the selection on every change.
 * @param state The query state.
 * @param eventStore The event store the query runs against.
 * @param resolution The time bucket size.
 * @returns The arguments for the histogram query.
 */
export const toHistogramArguments = (
    state: SequenceQueryState,
    eventStore: string,
    resolution: string
): SequenceHistogramParameters => ({
    eventStore,
    namespace: state.namespace,
    eventSequenceId: state.eventSequenceId,
    resolution,
    ...narrowing(state)
});
