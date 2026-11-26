// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getNestedRows, resolveSelection } from '../nestedRows';

describe('when selecting a row', () => {
    const root = { _id: 'r1', items: [{ n: 1 }] };

    it('should select a root row', () => (resolveSelection(false, root, null) === root).should.be.true);
    it('should clear the selection when the selected root row is activated again', () => (resolveSelection(false, root, root) === null).should.be.true);
    it('should select a child of the already selected root row after drilling into it', () => {
        const child = getNestedRows([root], ['items'], 0, 10).rows[0];
        (resolveSelection(true, child, root) === root).should.be.true;
    });
    it('should select nothing for no row', () => (resolveSelection(true, null, root) === null).should.be.true);
});
