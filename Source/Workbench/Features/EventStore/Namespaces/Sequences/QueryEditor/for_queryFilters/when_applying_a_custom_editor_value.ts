// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import {
    applyCustomFilterValue,
    correlationFilterKey,
    eventSourceFilterKey,
    eventSourceTypeFilterKey,
    eventStreamTypeFilterKey,
    occurredFilterKey,
    tagsFilterKey,
    toCustomFilterValues
} from '../queryFilters';

const state = createSequenceQueryState('the-id', 'The query', 'default');

describe('when applying an event source', () => {
    it('should narrow on it', () =>
        applyCustomFilterValue(state, eventSourceFilterKey, 'the-source').eventSourceId.should.equal('the-source'));
});

describe('when applying an event source type', () => {
    it('should narrow on it', () =>
        applyCustomFilterValue(state, eventSourceTypeFilterKey, 'Account').eventSourceType.should.equal('Account'));
});

describe('when applying an event stream type', () => {
    it('should narrow on it', () =>
        applyCustomFilterValue(state, eventStreamTypeFilterKey, 'Onboarding').eventStreamType.should.equal('Onboarding'));
});

describe('when applying a correlation', () => {
    it('should narrow on it', () =>
        applyCustomFilterValue(state, correlationFilterKey, 'the-correlation').correlationId.should.equal('the-correlation'));
});

describe('when applying tags', () => {
    it('should narrow on them', () =>
        applyCustomFilterValue(state, tagsFilterKey, ['important']).tags.should.eql(['important']));
});

describe('when applying a time range', () => {
    const result = applyCustomFilterValue(state, occurredFilterKey, [1, 2]);

    it('should take the lower bound from the range', () => result.occurredFrom!.should.equal(1));
    it('should take the upper bound from the range', () => result.occurredTo!.should.equal(2));
});

/**
 * A group only shows its clear button when it carries a value, so a dimension that narrows nothing
 * has to project to undefined rather than to an empty string or an empty array.
 */
describe('when projecting a query that narrows nothing onto the editors', () => {
    const values = toCustomFilterValues(state);

    it('should leave the event source unset', () => (values[eventSourceFilterKey] === undefined).should.be.true);
    it('should leave the event source type unset', () => (values[eventSourceTypeFilterKey] === undefined).should.be.true);
    it('should leave the event stream type unset', () => (values[eventStreamTypeFilterKey] === undefined).should.be.true);
    it('should leave the correlation unset', () => (values[correlationFilterKey] === undefined).should.be.true);
    it('should leave the tags unset', () => (values[tagsFilterKey] === undefined).should.be.true);
    it('should leave the time range unset', () => (values[occurredFilterKey] === undefined).should.be.true);
});

describe('when projecting a query that is bounded at only one end of the time range', () => {
    const values = toCustomFilterValues({ ...state, occurredFrom: 1 });

    it('should leave the time range unset, since the picker works on a whole range', () =>
        (values[occurredFilterKey] === undefined).should.be.true);
});
