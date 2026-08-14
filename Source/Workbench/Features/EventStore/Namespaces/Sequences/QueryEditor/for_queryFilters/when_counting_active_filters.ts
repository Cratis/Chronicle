// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import { countActiveFilters } from '../queryFilters';

const state = createSequenceQueryState('the-id', 'The query', 'default');

describe('when the query narrows nothing', () => {
    it('should count no active filters', () => countActiveFilters(state).should.equal(0));
});

describe('when the query narrows on several dimensions', () => {
    const count = countActiveFilters({
        ...state,
        eventTypes: ['Registered', 'Archived'],
        eventSourceId: 'the-source',
        occurredFrom: 1,
        occurredTo: 2
    });

    it('should count each selected event type and each other group once', () => count.should.equal(4));
});

describe('when the query narrows on every dimension the pivot viewer offers', () => {
    const count = countActiveFilters({
        ...state,
        eventTypes: ['Registered'],
        tags: ['important', 'audited'],
        eventSourceId: 'the-source',
        eventSourceType: 'the-source-type',
        eventStreamType: 'the-stream-type',
        correlationId: 'the-correlation',
        occurredFrom: 1,
        occurredTo: 2
    });

    it('should count each selected value and each other group once', () => count.should.equal(8));
});

describe('when the query has only whitespace as its event source', () => {
    it('should not count it as narrowing', () => countActiveFilters({ ...state, eventSourceId: '  ' }).should.equal(0));
});

describe('when the query is bounded at only one end of the time range', () => {
    it('should count the time range once', () => countActiveFilters({ ...state, occurredFrom: 1 }).should.equal(1));
});
