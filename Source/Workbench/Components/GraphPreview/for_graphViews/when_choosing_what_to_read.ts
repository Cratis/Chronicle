// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { clausesFor, rootFor, withRowIds } from '../graphViews';
import { organization } from './given/organization';

describe('when choosing what to read', () => {
    it('should start at the first label when none is chosen', () => rootFor(organization, { depth: 1, value: '' })!.should.equal('Person'));
    it('should start at the chosen label', () => rootFor(organization, { root: 'Group', depth: 1, value: '' })!.should.equal('Group'));
    it('should start at the first label when the chosen one is gone', () => rootFor(organization, { root: 'Nobody', depth: 1, value: '' })!.should.equal('Person'));
    it('should not filter without a property', () => clausesFor({ depth: 1, value: 'Ada' }).should.deep.equal([]));
    it('should not filter without a value', () => clausesFor({ depth: 1, property: 'name', value: '  ' }).should.deep.equal([]));
    it('should filter on the property and the trimmed value', () => clausesFor({ depth: 1, property: 'name', value: ' Ada ' }).should.deep.equal([{ member: 'name', value: 'Ada' }]));
    it('should key a row by the key of its root', () => withRowIds([{ personId: 'p1', name: 'Ada' }], 'personId')[0]._id!.should.equal('p1'));
    it('should key a row without a key by its position', () => withRowIds([{ name: 'Ada' }, { name: 'Grace' }], 'personId')[1]._id!.should.equal('1'));
});
