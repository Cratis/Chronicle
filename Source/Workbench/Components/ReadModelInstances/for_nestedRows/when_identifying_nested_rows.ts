// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getNestedRows } from '../nestedRows';

describe('when identifying nested rows', () => {
    const sourceA = { _id: 'a', items: [{ _id: 'dup' }, { _id: 'dup' }, {}] };
    const sourceB = { _id: 'b', items: [{ _id: 'dup' }] };
    const rows = getNestedRows([sourceA, sourceB], ['items'], 0, 10).rows;

    it('should give every row a unique key despite duplicate and missing ids', () => new Set(rows.map(r => r.__nestedKey)).size.should.equal(4));
    it('should number the keys by position in the whole collection', () => rows.map(r => r.__nestedKey).should.deep.equal(['nested-0', 'nested-1', 'nested-2', 'nested-3']));
    it('should keep keys stable on a later page', () => getNestedRows([sourceA, sourceB], ['items'], 3, 10).rows[0].__nestedKey.should.equal('nested-3'));
    it('should keep the original source document of each row', () => (rows[0].__sourceInstance === sourceA && rows[3].__sourceInstance === sourceB).should.be.true);
    it('should not let a field named like the metadata replace it', () => {
        const row = getNestedRows([{ a: [{ __nestedKey: 'x', __sourceInstance: 'y' }] }], ['a'], 0, 1).rows[0];
        [row.__nestedKey, typeof row.__sourceInstance].should.deep.equal(['nested-0', 'object']);
    });
});
