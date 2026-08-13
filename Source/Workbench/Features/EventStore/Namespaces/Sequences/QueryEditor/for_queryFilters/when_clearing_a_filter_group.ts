// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import { clearFilter, eventSourceFilterKey, eventTypeFilterKey, occurredFilterKey } from '../queryFilters';

const state = {
    ...createSequenceQueryState('the-id', 'The query', 'default'),
    eventSourceId: 'the-source',
    eventTypes: ['Registered'],
    occurredFrom: 1,
    occurredTo: 2
};

describe('when clearing the event type group', () => {
    const result = clearFilter(state, eventTypeFilterKey);

    it('should stop narrowing on event types', () => result.eventTypes.length.should.equal(0));
    it('should leave the event source narrowing alone', () => result.eventSourceId.should.equal('the-source'));
});

describe('when clearing the event source group', () => {
    const result = clearFilter(state, eventSourceFilterKey);

    it('should stop narrowing on event source', () => result.eventSourceId.should.equal(''));
    it('should leave the event type narrowing alone', () => result.eventTypes.should.eql(['Registered']));
});

describe('when clearing the time range group', () => {
    const result = clearFilter(state, occurredFilterKey);

    it('should drop the lower bound', () => (result.occurredFrom === undefined).should.be.true);
    it('should drop the upper bound', () => (result.occurredTo === undefined).should.be.true);
});

describe('when clearing a group the query does not know', () => {
    it('should leave the query untouched', () => clearFilter(state, 'something-else').should.equal(state));
});
