// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { ObserveEventStores } from '../../../generated-input/EventStores/ObserveEventStores';
import { useEventStoreNames } from '../../../src/event-stores';

describe('when observing event stores and no event stores exist', () => {
    let result: string[];

    beforeAll(() => {
        vi.spyOn(ObserveEventStores, 'use').mockReturnValue([{ data: [] }] as unknown as ReturnType<typeof ObserveEventStores.use>);
        result = useEventStoreNames();
    });

    afterAll(() => vi.restoreAllMocks());

    it('should return no names', () => {
        result.should.deep.equal([]);
    });
});
