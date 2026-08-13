// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { buildQueryTree } from '../buildQueryTree';
import { QueryNodeKind } from '../QueryNodeKind';

const queryWith = (id: string, name: string, scope: SequenceQueryScope, folder: string): SequenceQuery => {
    const query = new SequenceQuery();
    query.id = id;
    query.name = name;
    query.scope = scope;
    query.folder = folder;
    return query;
};

describe('when nothing has been saved', () => {
    const roots = buildQueryTree([], 'Only me', 'Everyone');

    it('should still offer both scopes', () => roots.length.should.equal(2));
    it('should name the first root for the current user', () => roots[0].name.should.equal('Only me'));
    it('should name the second root for everyone', () => roots[1].name.should.equal('Everyone'));
    it('should leave the roots empty', () => roots[0].children.length.should.equal(0));
});

describe('when a query sits directly under its scope', () => {
    const roots = buildQueryTree(
        [queryWith('1', 'Recent appends', SequenceQueryScope.user, '')],
        'Only me',
        'Everyone');

    it('should hang the query straight off the root', () => roots[0].children.length.should.equal(1));
    it('should hang it as a query rather than a folder', () => roots[0].children[0].kind.should.equal(QueryNodeKind.Query));
    it('should keep the query name', () => roots[0].children[0].name.should.equal('Recent appends'));
});

describe('when a query is filed several folders deep', () => {
    const roots = buildQueryTree(
        [queryWith('1', 'Failed appends', SequenceQueryScope.user, 'Diagnostics/Failures')],
        'Only me',
        'Everyone');

    const diagnostics = roots[0].children[0];
    const failures = diagnostics.children[0];

    it('should create the outer folder', () => diagnostics.name.should.equal('Diagnostics'));
    it('should create the inner folder', () => failures.name.should.equal('Failures'));
    it('should give the inner folder its full path', () => failures.folder.should.equal('Diagnostics/Failures'));
    it('should put the query in the innermost folder', () => failures.children[0].name.should.equal('Failed appends'));
});

describe('when two queries share a folder', () => {
    const roots = buildQueryTree(
        [
            queryWith('1', 'Failed appends', SequenceQueryScope.user, 'Diagnostics'),
            queryWith('2', 'Slow appends', SequenceQueryScope.user, 'Diagnostics')
        ],
        'Only me',
        'Everyone');

    it('should create the folder once', () => roots[0].children.length.should.equal(1));
    it('should put both queries in it', () => roots[0].children[0].children.length.should.equal(2));
});

describe('when queries belong to different scopes', () => {
    const roots = buildQueryTree(
        [
            queryWith('1', 'Mine', SequenceQueryScope.user, ''),
            queryWith('2', 'Shared', SequenceQueryScope.everyone, '')
        ],
        'Only me',
        'Everyone');

    it('should file the private one under its own root', () => roots[0].children[0].name.should.equal('Mine'));
    it('should file the shared one under the everyone root', () => roots[1].children[0].name.should.equal('Shared'));
});

describe('when a folder and a query sit side by side', () => {
    const roots = buildQueryTree(
        [
            queryWith('1', 'Aardvark', SequenceQueryScope.user, ''),
            queryWith('2', 'Anything', SequenceQueryScope.user, 'Zebra')
        ],
        'Only me',
        'Everyone');

    it('should order the folder ahead of the query', () => roots[0].children[0].kind.should.equal(QueryNodeKind.Folder));
    it('should order the query after the folder', () => roots[0].children[1].kind.should.equal(QueryNodeKind.Query));
});
