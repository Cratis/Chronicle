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
    query.folder = '';
    query.namespace = 'default';
    query.eventSequenceId = 'event-log';
    query.eventSourceId = '';
    query.eventSourceType = '';
    query.eventStreamType = '';
    query.correlationId = '';
    query.eventTypes = [];
    query.tags = [];
    query.sortBy = 'sequenceNumber';
    query.descending = true;
    return query;
};

describe('when nothing is open and nothing is saved', () => {
    const result = reconcileOpenQueries([], [], 'New query', 'generated-id', 'default');

    it('should open a single query so the page is never empty', () => result.open.length.should.equal(1));
    it('should give it the name for a new query', () => result.open[0].state.name.should.equal('New query'));
    it('should give it the generated identifier', () => result.open[0].state.id.should.equal('generated-id'));
    it('should run it against the namespace being viewed', () => result.open[0].state.namespace.should.equal('default'));
    it('should have nothing saved to compare against', () => (result.open[0].saved === null).should.be.true);
});

describe('when nothing is open but queries are saved', () => {
    const result = reconcileOpenQueries(
        [savedQuery('first', 'First'), savedQuery('second', 'Second')], [], 'New query', 'generated-id', 'default');

    it('should open the first saved query', () => result.open.length.should.equal(1));
    it('should open it by its saved identity', () => result.open[0].state.id.should.equal('first'));
    it('should open it under its saved name', () => result.open[0].state.name.should.equal('First'));
    it('should treat it as having nothing to save', () => result.open[0].saved!.name.should.equal('First'));
});

/**
 * Saved queries are re-read every time one is written back, so this fires again after every save.
 * Reseeding from the server copy at that point would throw away whatever the user is typing.
 */
describe('when queries are already open', () => {
    const open = [{ state: createSequenceQueryState('being-edited', 'Half typed na', 'default'), saved: null }];
    const result = reconcileOpenQueries([savedQuery('being-edited', 'Half typed name')], open, 'New query', 'generated-id', 'default');

    it('should keep exactly what is open', () => result.open.should.equal(open));
    it('should not overwrite in-flight edits with the saved copy', () => result.open[0].state.name.should.equal('Half typed na'));
    it('should leave the active tab alone', () => result.activeIndex.should.equal(-1));
});

/**
 * The workspace is meant to be picked up where it was left, so what was open last time wins over
 * simply opening the first saved query.
 */
describe('when queries were open the last time', () => {
    const result = reconcileOpenQueries(
        [savedQuery('first', 'First'), savedQuery('second', 'Second'), savedQuery('third', 'Third')],
        [],
        'New query',
        'generated-id',
        'default',
        { ids: ['third', 'first'], activeIndex: 1 });

    it('should reopen them', () => result.open.length.should.equal(2));
    it('should reopen them in the order they were in', () => result.open[0].state.id.should.equal('third'));
    it('should show the one that was being looked at', () => result.activeIndex.should.equal(1));
});

describe('when a query that was open has since been deleted', () => {
    const result = reconcileOpenQueries(
        [savedQuery('first', 'First')],
        [],
        'New query',
        'generated-id',
        'default',
        { ids: ['gone', 'first'], activeIndex: 0 });

    it('should leave the deleted one out', () => result.open.length.should.equal(1));
    it('should reopen the one that survived', () => result.open[0].state.id.should.equal('first'));
    it('should fall back to a tab that exists', () => result.activeIndex.should.equal(0));
});

describe('when every query that was open has since been deleted', () => {
    const result = reconcileOpenQueries(
        [savedQuery('first', 'First')],
        [],
        'New query',
        'generated-id',
        'default',
        { ids: ['gone'], activeIndex: 0 });

    it('should fall back to opening a saved query', () => result.open[0].state.id.should.equal('first'));
});
