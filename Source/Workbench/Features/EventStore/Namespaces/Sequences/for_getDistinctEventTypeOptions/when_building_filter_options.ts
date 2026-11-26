// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventType, EventTypeOwner, EventTypeSource, EventTypeVisibility } from 'Features/Contracts/Events';
import { EventTypeDetails } from 'Features/EventTypes';
import { getDistinctEventTypeOptions } from '../getDistinctEventTypeOptions';

const eventTypeWith = (id: string, generation: number, visibility = EventTypeVisibility.unspecified): EventTypeDetails => {
    const eventType = new EventType();
    eventType.id = id;
    eventType.generation = generation;
    eventType.tombstone = false;

    const details = new EventTypeDetails();
    details.type = eventType;
    details.owner = EventTypeOwner.client;
    details.source = EventTypeSource.code;
    details.schema = '{}';
    details.visibility = visibility;
    return details;
};

describe('when building filter options from event types', () => {
    const options = getDistinctEventTypeOptions([
        eventTypeWith('Orders.OrderPlaced', 1),
        eventTypeWith('Accounts.AccountOpened', 1),
    ]);

    it('should return an option per distinct event type', () => options.length.should.equal(2));
    it('should sort the options alphabetically by id', () => options[0].value.should.equal('Accounts.AccountOpened'));
    it('should use the event type id as the label', () => options[1].label.should.equal('Orders.OrderPlaced'));
    it('should use the event type id as the value', () => options[1].value.should.equal('Orders.OrderPlaced'));
});

describe('when event types have multiple generations of the same id', () => {
    const options = getDistinctEventTypeOptions([
        eventTypeWith('Orders.OrderPlaced', 1),
        eventTypeWith('Orders.OrderPlaced', 2),
    ]);

    it('should collapse them into a single option', () => options.length.should.equal(1));
    it('should keep the shared id as the value', () => options[0].value.should.equal('Orders.OrderPlaced'));
});

describe('when there are no event types', () => {
    const options = getDistinctEventTypeOptions([]);

    it('should return no options', () => options.length.should.equal(0));
});

describe('when building options for the outbox', () => {
    const eventTypes = [
        eventTypeWith('Orders.OrderPlaced', 1, EventTypeVisibility.public),
        eventTypeWith('Orders.OrderCancelled', 1, EventTypeVisibility.private),
        eventTypeWith('Orders.OrderLegacy', 1, EventTypeVisibility.unspecified),
        eventTypeWith('Accounts.AccountOpened', 1, EventTypeVisibility.public),
        eventTypeWith('Accounts.AccountOpened', 2, EventTypeVisibility.public),
    ];
    const options = getDistinctEventTypeOptions(eventTypes, 'outbox');

    it('should only offer public types, sorted and deduplicated', () => options.map(option => option.value).should.deep.equal(['Accounts.AccountOpened', 'Orders.OrderPlaced']));
    it('should offer every type for the event log', () => getDistinctEventTypeOptions(eventTypes, 'event-log').length.should.equal(4));
    it('should offer every type when no sequence is given', () => getDistinctEventTypeOptions(eventTypes).length.should.equal(4));
    it('should offer nothing for an empty outbox catalog', () => getDistinctEventTypeOptions([], 'outbox').length.should.equal(0));
});
