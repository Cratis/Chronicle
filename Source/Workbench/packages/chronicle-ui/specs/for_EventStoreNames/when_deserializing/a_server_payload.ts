// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { JsonSerializer } from '@cratis/fundamentals';
import { EventStoreNames } from '../../../generated-input/EventStores/EventStoreNames';

describe('when deserializing a server payload into the generated event store names type', () => {
    let result: EventStoreNames[];

    beforeAll(() => {
        result = JsonSerializer.deserializeArray(EventStoreNames, JSON.stringify([{ id: 'first', name: 'first' }, { id: 'second', name: 'second' }]));
    });

    it('should produce instances of the generated type', () => {
        result.every(item => item instanceof EventStoreNames).should.be.true;
    });

    it('should carry the field values', () => {
        result.map(item => item.name).should.deep.equal(['first', 'second']);
    });
});
