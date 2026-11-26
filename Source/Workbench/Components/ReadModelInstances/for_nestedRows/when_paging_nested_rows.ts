// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getNestedRows } from '../nestedRows';

const parent = (children: unknown[]) => ({ _id: 'p', children });
const children = (count: number) => Array.from({ length: count }, (_, i) => ({ _id: 'same', n: i }));

describe('when paging nested rows', () => {
    it('should count the whole collection', () => getNestedRows([parent(children(1000))], ['children'], 0, 50).total.should.equal(1000));
    it('should build no more than a page', () => getNestedRows([parent(children(1000))], ['children'], 0, 50).rows.length.should.equal(50));
    it('should start the middle page at its offset', () => getNestedRows([parent(children(1000))], ['children'], 500, 50).rows[0].n!.should.equal(500));
    it('should end the final page at the last row', () => {
        const result = getNestedRows([parent(children(120))], ['children'], 100, 50);
        [result.rows.length, result.rows[19].n].should.deep.equal([20, 119]);
    });
    it('should build nothing for a page past the end', () => getNestedRows([parent(children(10))], ['children'], 50, 50).rows.length.should.equal(0));
    it('should build nothing for a zero size window yet still count', () => getNestedRows([parent(children(10))], ['children'], 0, 0).total.should.equal(10));
    it('should page across several documents', () => {
        const result = getNestedRows([parent(children(30)), parent(children(30))], ['children'], 25, 10);
        [result.total, result.rows.map(r => r.n).join()].should.deep.equal([60, '25,26,27,28,29,0,1,2,3,4']);
    });
    it('should not read the fields of rows outside the window', () => {
        let reads = 0;
        const watched = Array.from({ length: 100 }, (_, i) => Object.defineProperty({}, 'n', { enumerable: true, get: () => { reads++; return i; } }));
        getNestedRows([parent(watched)], ['children'], 0, 5);
        reads.should.be.at.most(10);
    });
});
