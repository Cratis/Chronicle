// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventType } from 'Api/Events';
import { getDistinctEventTypeOptions } from '../getDistinctEventTypeOptions';

const eventTypeWith = (id: string, generation: number): EventType => {
    const eventType = new EventType();
    eventType.id = id;
    eventType.generation = generation;
    eventType.tombstone = false;
    return eventType;
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
