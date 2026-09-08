// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveMenuItemParams } from '../../resolveMenuItemParams';

describe('when resolving params and params and fallback share a key', () => {
    let result: Record<string, string | undefined>;

    beforeEach(() => {
        result = resolveMenuItemParams({ namespace: 'real-namespace' }, { namespace: 'stale-namespace' });
    });

    it('should use the real param value', () => {
        result.namespace!.should.equal('real-namespace');
    });
});
