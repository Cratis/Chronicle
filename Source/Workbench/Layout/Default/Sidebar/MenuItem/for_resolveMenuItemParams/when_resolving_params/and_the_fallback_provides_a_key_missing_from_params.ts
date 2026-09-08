// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveMenuItemParams } from '../../resolveMenuItemParams';

describe('when resolving params and the fallback provides a key missing from params', () => {
    let result: Record<string, string | undefined>;

    beforeEach(() => {
        result = resolveMenuItemParams({ eventStore: 'store-a' }, { namespace: 'fallback-namespace' });
    });

    it('should fill in the value from the fallback', () => {
        result.namespace!.should.equal('fallback-namespace');
    });

    it('should keep the real param value', () => {
        result.eventStore!.should.equal('store-a');
    });
});
