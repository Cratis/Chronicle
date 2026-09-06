// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Features/SequenceQueries';
import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { stillOpenable } from '../rememberedQueries';

const savedQuery = (id: string): SequenceQuery => {
    const query = new SequenceQuery();
    query.id = id;
    query.name = id;
    query.scope = SequenceQueryScope.user;
    return query;
};

const saved = [savedQuery('first'), savedQuery('second'), savedQuery('third')];

describe('when everything that was open still exists', () => {
    const result = stillOpenable({ ids: ['third', 'first'], activeIndex: 1 }, saved);

    it('should reopen all of them', () => result.queries.length.should.equal(2));
    it('should keep the order they were in rather than the saved order', () =>
        result.queries.map(query => query.id).should.eql(['third', 'first']));
    it('should stay on the query that was being looked at', () => result.activeIndex.should.equal(1));
});

/**
 * A query can be deleted between visits - by this user, or by somebody else when it was shared with
 * everyone - so what comes back has to be reconciled against what still exists.
 */
describe('when one that was open has since been deleted', () => {
    const result = stillOpenable({ ids: ['gone', 'second', 'third'], activeIndex: 2 }, saved);

    it('should leave the deleted one out', () => result.queries.map(query => query.id).should.eql(['second', 'third']));
    it('should follow the query that was being looked at to its new position', () => result.activeIndex.should.equal(1));
});

describe('when the query that was being looked at has since been deleted', () => {
    const result = stillOpenable({ ids: ['first', 'gone'], activeIndex: 1 }, saved);

    it('should reopen what is left', () => result.queries.map(query => query.id).should.eql(['first']));
    it('should fall back to a tab that exists', () => result.activeIndex.should.equal(0));
});

describe('when everything that was open has since been deleted', () => {
    const result = stillOpenable({ ids: ['gone', 'also-gone'], activeIndex: 0 }, saved);

    it('should reopen nothing', () => result.queries.length.should.equal(0));
});
