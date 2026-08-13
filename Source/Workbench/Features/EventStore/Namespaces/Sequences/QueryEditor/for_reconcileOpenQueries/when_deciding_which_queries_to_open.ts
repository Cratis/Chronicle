// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { createSequenceQueryState } from '../SequenceQueryState';
import { reconcileOpenQueries } from '../useOpenQueries';

const savedQuery = (id: string, name: string): SequenceQuery => {
    const query = new SequenceQuery();
    query.id = id;
    query.name = name;
    query.scope = SequenceQueryScope.user;
    query.namespace = 'default';
    query.eventSequenceId = 'event-log';
    query.eventSourceId = '';
    query.eventTypes = [];
    query.tags = [];
    query.descending = true;
    return query;
};

describe('when nothing is open and nothing is saved', () => {
    const result = reconcileOpenQueries([], [], 'New query', 'generated-id', 'default');

    it('should open a single query so the page is never empty', () => result.length.should.equal(1));
    it('should give it the name for a new query', () => result[0].name.should.equal('New query'));
    it('should give it the generated identifier', () => result[0].id.should.equal('generated-id'));
    it('should run it against the namespace being viewed', () => result[0].namespace.should.equal('default'));
});

describe('when nothing is open but queries are saved', () => {
    const result = reconcileOpenQueries(
        [savedQuery('first', 'First'), savedQuery('second', 'Second')], [], 'New query', 'generated-id', 'default');

    it('should open the first saved query', () => result.length.should.equal(1));
    it('should open it by its saved identity', () => result[0].id.should.equal('first'));
    it('should open it under its saved name', () => result[0].name.should.equal('First'));
});

/**
 * Saved queries arrive over a live query, so this fires again every time an edit is written back.
 * Reseeding from the server copy at that point would throw away whatever the user is typing.
 */
describe('when queries are already open', () => {
    const open = [createSequenceQueryState('being-edited', 'Half typed na', 'default')];
    const result = reconcileOpenQueries([savedQuery('being-edited', 'Half typed name')], open, 'New query', 'generated-id', 'default');

    it('should keep exactly what is open', () => result.should.equal(open));
    it('should not overwrite in-flight edits with the saved copy', () => result[0].name.should.equal('Half typed na'));
});
