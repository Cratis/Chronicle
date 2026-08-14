// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import { toQueryArguments } from '../toQueryArguments';

/**
 * A query with no filters set has to leave every dimension unnarrowed. Sending empty strings instead
 * would make the backend match on an empty value and silently return nothing. Ordering is not here
 * at all - Arc carries that on the query rather than as an argument.
 */
describe('when the query narrows nothing', () => {
    const args = toQueryArguments(createSequenceQueryState('the-id', 'The query', 'default'), 'the-store');

    it('should not narrow on event source', () => (args.eventSourceId === undefined).should.be.true);
    it('should not narrow on event types', () => (args.eventTypeIds === undefined).should.be.true);
    it('should not bound the lower end of the time range', () => (args.occurredFrom === undefined).should.be.true);
    it('should not bound the upper end of the time range', () => (args.occurredTo === undefined).should.be.true);
    it('should carry the event store', () => args.eventStore.should.equal('the-store'));
    it('should carry the namespace', () => args.namespace.should.equal('default'));
    it('should carry the event sequence', () => args.eventSequenceId.should.equal('event-log'));
});

describe('when the query has only whitespace as its event source', () => {
    const state = { ...createSequenceQueryState('the-id', 'The query', 'default'), eventSourceId: '   ' };
    const args = toQueryArguments(state, 'the-store');

    it('should not narrow on event source', () => (args.eventSourceId === undefined).should.be.true);
});
