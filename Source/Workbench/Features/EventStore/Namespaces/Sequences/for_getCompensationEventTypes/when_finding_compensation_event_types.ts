// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventType, EventTypeOwner, EventTypeSource } from 'Features/Contracts/Events';
import { EventTypeDetails } from 'Features/EventTypes';
import { getCompensationEventTypes } from '../getCompensationEventTypes';

const eventTypeWith = (id: string, generation: number, schema: string): EventTypeDetails => {
    const eventType = new EventType();
    eventType.id = id;
    eventType.generation = generation;
    eventType.tombstone = false;

    const details = new EventTypeDetails();
    details.type = eventType;
    details.owner = EventTypeOwner.client;
    details.source = EventTypeSource.code;
    details.schema = schema;
    return details;
};

const compensating = (target: string) => JSON.stringify({ type: 'object', compensationFor: target });

describe('when finding compensation event types for an event type', () => {
    const result = getCompensationEventTypes([
        eventTypeWith('Ledger.EntryReversed', 1, compensating('Ledger.EntryPosted')),
        eventTypeWith('Ledger.EntryCorrected', 1, compensating('Ledger.EntryPosted')),
        eventTypeWith('Ledger.OtherReversed', 1, compensating('Ledger.OtherPosted')),
        eventTypeWith('Ledger.EntryPosted', 1, '{"type":"object"}')
    ], 'Ledger.EntryPosted');

    it('should only return the event types that compensate for it', () => result.length.should.equal(2));
    it('should sort them alphabetically by id', () => result[0].type.id.should.equal('Ledger.EntryCorrected'));
    it('should include every compensating event type', () => result[1].type.id.should.equal('Ledger.EntryReversed'));
});

describe('when a compensation event type has several generations', () => {
    const result = getCompensationEventTypes([
        eventTypeWith('Ledger.EntryReversed', 1, compensating('Ledger.EntryPosted')),
        eventTypeWith('Ledger.EntryReversed', 2, compensating('Ledger.EntryPosted'))
    ], 'Ledger.EntryPosted');

    it('should return it once', () => result.length.should.equal(1));
    it('should keep the latest generation', () => result[0].type.generation.should.equal(2));
});

describe('when no event type compensates for it', () => {
    const result = getCompensationEventTypes([eventTypeWith('Ledger.EntryPosted', 1, '{}')], 'Ledger.EntryPosted');

    it('should return nothing', () => result.length.should.equal(0));
});

describe('when a schema is not valid JSON', () => {
    const result = getCompensationEventTypes([eventTypeWith('Ledger.Broken', 1, 'not json')], 'Ledger.EntryPosted');

    it('should ignore the event type', () => result.length.should.equal(0));
});
