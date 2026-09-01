// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { maximumProperties, summarizeProperties } from '../eventProperties';

const manyProperties = (count: number) =>
    Object.fromEntries(Array.from({ length: count }, (_, index) => [`property${index}`, index]));

describe('when summarizing an events properties', () => {
    it('should summarize nothing for an event with no content', () =>
        summarizeProperties({}).properties.length.should.equal(0));

    it('should survive absent content', () => summarizeProperties(null).properties.length.should.equal(0));

    it('should keep every property when they fit', () =>
        summarizeProperties({ name: 'Acme', total: 3 }).properties
            .should.eql([{ name: 'name', value: 'Acme' }, { name: 'total', value: '3' }]));

    it('should leave nothing out when they fit', () =>
        summarizeProperties({ name: 'Acme' }).remaining.should.equal(0));

    it('should stop at the limit', () =>
        summarizeProperties(manyProperties(30)).properties.length.should.equal(maximumProperties));

    it('should say how many it left out', () =>
        summarizeProperties(manyProperties(30)).remaining.should.equal(30 - maximumProperties));

    it('should leave nothing out at exactly the limit', () =>
        summarizeProperties(manyProperties(maximumProperties)).remaining.should.equal(0));
});
