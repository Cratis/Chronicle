// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { clampNestedPage, nestedPageCount, resolvePaging } from '../nestedPaging';

const root = { page: 3, pageSize: 25, totalItems: 1000 };

describe('when resolving paging', () => {
    it('should show the root paging when not nested', () => resolvePaging(false, root, { page: 0, pageSize: 50, totalItems: 120 }).should.deep.equal({ ...root, pageCount: 40 }));
    it('should show the local paging when nested', () => resolvePaging(true, root, { page: 1, pageSize: 50, totalItems: 120 }).should.deep.equal({ page: 1, pageSize: 50, totalItems: 120, pageCount: 3 }));
    it('should clamp a nested page past the end', () => resolvePaging(true, root, { page: 9, pageSize: 50, totalItems: 120 }).page.should.equal(2));
    it('should not change the root paging while showing nested paging', () => {
        resolvePaging(true, root, { page: 1, pageSize: 10, totalItems: 5 });
        root.should.deep.equal({ page: 3, pageSize: 25, totalItems: 1000 });
    });
    it('should count an empty collection as one page', () => nestedPageCount(0, 50).should.equal(1));
    it('should clamp a negative page to the first', () => clampNestedPage(-4, 100, 10).should.equal(0));
});
