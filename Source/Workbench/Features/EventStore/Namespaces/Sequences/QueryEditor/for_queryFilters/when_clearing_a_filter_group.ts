// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import {
    clearFilter,
    correlationFilterKey,
    eventSourceFilterKey,
    eventSourceTypeFilterKey,
    eventStreamTypeFilterKey,
    eventTypeFilterKey,
    occurredFilterKey,
    tagsFilterKey
} from '../queryFilters';

const state = {
    ...createSequenceQueryState('the-id', 'The query', 'default'),
    eventSourceId: 'the-source',
    eventSourceType: 'the-source-type',
    eventStreamType: 'the-stream-type',
    correlationId: 'the-correlation',
    eventTypes: ['Registered'],
    tags: ['important'],
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

describe('when clearing the event source type group', () => {
    const result = clearFilter(state, eventSourceTypeFilterKey);

    it('should stop narrowing on event source type', () => result.eventSourceType.should.equal(''));
    it('should leave the event source narrowing alone', () => result.eventSourceId.should.equal('the-source'));
});

describe('when clearing the event stream type group', () => {
    const result = clearFilter(state, eventStreamTypeFilterKey);

    it('should stop narrowing on event stream type', () => result.eventStreamType.should.equal(''));
});

describe('when clearing the correlation group', () => {
    const result = clearFilter(state, correlationFilterKey);

    it('should stop narrowing on correlation', () => result.correlationId.should.equal(''));
});

describe('when clearing the tags group', () => {
    const result = clearFilter(state, tagsFilterKey);

    it('should stop narrowing on tags', () => result.tags.length.should.equal(0));
});

describe('when clearing the time range group', () => {
    const result = clearFilter(state, occurredFilterKey);

    it('should drop the lower bound', () => (result.occurredFrom === undefined).should.be.true);
    it('should drop the upper bound', () => (result.occurredTo === undefined).should.be.true);
});

describe('when clearing a group the query does not know', () => {
    it('should leave the query untouched', () => clearFilter(state, 'something-else').should.equal(state));
});
