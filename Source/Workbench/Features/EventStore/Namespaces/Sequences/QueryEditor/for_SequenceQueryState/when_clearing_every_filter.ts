// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState, hasAnyFilter, withoutFilters } from '../SequenceQueryState';

const narrowed = {
    ...createSequenceQueryState('the-id', 'The query', 'default'),
    eventSourceId: 'the-source',
    eventTypes: ['Registered'],
    tags: ['important'],
    occurredFrom: 1,
    occurredTo: 2
};

describe('when clearing every filter', () => {
    const result = withoutFilters(narrowed);

    it('should no longer narrow on anything', () => hasAnyFilter(result).should.be.false);
    it('should keep the query identity', () => result.id.should.equal('the-id'));
    it('should keep the query name', () => result.name.should.equal('The query'));
    it('should keep the event sequence', () => result.eventSequenceId.should.equal(narrowed.eventSequenceId));
    it('should keep the ordering', () => result.descending.should.equal(narrowed.descending));
});

describe('when asking whether a narrowed query has filters', () => {
    it('should say it does', () => hasAnyFilter(narrowed).should.be.true);
});
