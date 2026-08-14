// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createSequenceQueryState } from '../SequenceQueryState';
import { toHistogramArguments, toQueryArguments } from '../toQueryArguments';

const from = Date.UTC(2026, 7, 10);
const to = Date.UTC(2026, 7, 12);

const state = {
    ...createSequenceQueryState('the-id', 'The query', 'default'),
    eventSourceId: 'the-source',
    eventTypes: ['Registered', 'Archived'],
    occurredFrom: from,
    occurredTo: to,
    descending: false
};

describe('when the query narrows on every dimension', () => {
    const args = toQueryArguments(state, 'the-store');

    it('should narrow on the event source', () => args.eventSourceId!.should.equal('the-source'));
    it('should send the event types as a comma separated list', () => args.eventTypeIds!.should.equal('Registered,Archived'));
    it('should send the lower bound as a date', () => args.occurredFrom!.getTime().should.equal(from));
    it('should send the upper bound as a date', () => args.occurredTo!.getTime().should.equal(to));
});

/**
 * The histogram is what the user picks a time range with, so narrowing it by the range already
 * picked would collapse it to the selection and make the rest of the span unreachable.
 */
describe('when building the histogram arguments for that same query', () => {
    const args = toHistogramArguments(state, 'the-store', 'day');

    it('should carry the requested resolution', () => args.resolution!.should.equal('day'));
    it('should apply the event source narrowing', () => args.eventSourceId!.should.equal('the-source'));
    it('should apply the event type narrowing', () => args.eventTypeIds!.should.equal('Registered,Archived'));
    it('should not apply the selected time range', () => (args.occurredFrom === undefined).should.be.true);
    it('should not apply the selected upper bound either', () => (args.occurredTo === undefined).should.be.true);
});
