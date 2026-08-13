// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryEventsParameters } from 'Api/EventSequences/QueryEvents';
import { SequenceHistogramParameters } from 'Api/EventSequences/SequenceHistogram';
import { SequenceQueryState } from './SequenceQueryState';

/**
 * Project a query's state onto the arguments its event query takes.
 *
 * Absent filters are sent as undefined rather than as empty strings, so the backend leaves those
 * dimensions unnarrowed instead of matching on an empty value.
 * @param state The query state.
 * @param eventStore The event store the query runs against.
 * @returns The arguments for the events query.
 */
export const toQueryArguments = (state: SequenceQueryState, eventStore: string): QueryEventsParameters => ({
    eventStore,
    namespace: state.namespace,
    eventSequenceId: state.eventSequenceId,
    eventSourceId: state.eventSourceId.trim() || undefined,
    eventTypeIds: state.eventTypes.length > 0 ? state.eventTypes.join(',') : undefined,
    occurredFrom: state.occurredFrom !== undefined ? new Date(state.occurredFrom) : undefined,
    occurredTo: state.occurredTo !== undefined ? new Date(state.occurredTo) : undefined,
    descending: state.descending
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
    eventSourceId: state.eventSourceId.trim() || undefined,
    eventTypeIds: state.eventTypes.length > 0 ? state.eventTypes.join(',') : undefined
});
