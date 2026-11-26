// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getNestedRows } from '../nestedRows';

describe('when resolving nested rows', () => {
    const doc = { _id: 'd1', configurations: [{ hubs: [{ name: 'a' }, { name: 'b' }] }, { hubs: [{ name: 'c' }] }], single: { x: 1 }, tags: ['t1', null, 3] };

    it('should follow every element of an intermediate array', () => getNestedRows([doc], ['configurations', 'hubs'], 0, 10).rows.map(r => r.name).should.deep.equal(['a', 'b', 'c']));
    it('should keep array indices relative to their own array', () => getNestedRows([doc], ['configurations', 'hubs'], 0, 10).rows.map(r => r.__arrayIndex).should.deep.equal([0, 1, 0]));
    it('should show an object as a single row without an index', () => {
        const result = getNestedRows([doc], ['single'], 0, 10);
        [result.total, result.rows[0].x, 'x' in result.rows[0] && '__arrayIndex' in result.rows[0]].should.deep.equal([1, 1, false]);
    });
    it('should show primitive and null entries under value', () => getNestedRows([doc], ['tags'], 0, 10).rows.map(r => r.value).should.deep.equal(['t1', null, 3]));
    it('should find nothing on a missing path', () => getNestedRows([doc], ['nothing', 'here'], 0, 10).should.deep.equal({ total: 0, rows: [], columns: [] }));
    it('should find nothing for a path that ends at a primitive', () => getNestedRows([{ a: 1 }], ['a'], 0, 10).total.should.equal(0));
    it('should find nothing without documents', () => getNestedRows([], ['a'], 0, 10).total.should.equal(0));
    it('should skip documents without the path', () => getNestedRows([{ a: [{ n: 1 }] }, { b: 1 }, { a: [{ n: 2 }] }], ['a'], 0, 10).rows.map(r => r.n).should.deep.equal([1, 2]));
    it('should take the columns from the first row of the whole collection', () => getNestedRows([{ a: [{ first: 1, __hidden: 2 }, { other: 1 }] }], ['a'], 1, 1).columns.should.deep.equal(['first']));
    it('should take the columns from the first non empty collection', () => getNestedRows([{ a: [] }, { a: [{ k: 1 }] }], ['a'], 0, 10).columns.should.deep.equal(['k']));
});
