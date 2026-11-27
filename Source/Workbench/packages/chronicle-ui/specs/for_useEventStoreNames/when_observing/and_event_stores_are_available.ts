// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { ObserveEventStores } from '../../../generated-input/EventStores/ObserveEventStores';
import { EventStoreNames } from '../../../generated-input/EventStores/EventStoreNames';
import { useEventStoreNames } from '../../../src/event-stores';

const eventStore = (name: string): EventStoreNames => Object.assign(new EventStoreNames(), { id: name, name });

describe('when observing event stores and event stores are available', () => {
    let result: string[];

    beforeAll(() => {
        vi.spyOn(ObserveEventStores, 'use').mockReturnValue([{ data: [eventStore('zeta'), eventStore('alpha')] }] as unknown as ReturnType<typeof ObserveEventStores.use>);
        result = useEventStoreNames();
    });

    afterAll(() => vi.restoreAllMocks());

    it('should return the names in the order the query delivers them', () => {
        result.should.deep.equal(['zeta', 'alpha']);
    });
});
