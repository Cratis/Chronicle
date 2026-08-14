// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFolder } from 'Api/SequenceQueries/QueryFolder';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { buildQueryTree, foldersInScope } from '../buildQueryTree';
import { QueryNodeKind } from '../QueryNodeKind';

const folderAt = (scope: SequenceQueryScope, path: string): QueryFolder => {
    const folder = new QueryFolder();
    folder.id = path;
    folder.scope = scope;
    folder.owner = 'alice';
    folder.namespace = 'default';
    folder.path = path;
    return folder;
};

const queryWith = (id: string, name: string, scope: SequenceQueryScope, folder: string): SequenceQuery => {
    const query = new SequenceQuery();
    query.id = id;
    query.name = name;
    query.scope = scope;
    query.folder = folder;
    return query;
};

/**
 * A folder is stored in its own right, so one holding nothing still has to appear - creating a
 * folder before deciding what goes in it is the normal order of doing things.
 */
describe('when a folder holds nothing yet', () => {
    const roots = buildQueryTree(
        [],
        'Only me',
        'Everyone',
        [folderAt(SequenceQueryScope.user, 'Diagnostics')]);

    it('should still show the folder', () => roots[0].children.length.should.equal(1));
    it('should show it as a folder', () => roots[0].children[0].kind.should.equal(QueryNodeKind.Folder));
    it('should name it after its path', () => roots[0].children[0].name.should.equal('Diagnostics'));
    it('should leave the other scope empty', () => roots[1].children.length.should.equal(0));
});

describe('when an empty folder is nested inside a folder that holds queries', () => {
    const roots = buildQueryTree(
        [queryWith('1', 'Failed appends', SequenceQueryScope.user, 'Diagnostics')],
        'Only me',
        'Everyone',
        [folderAt(SequenceQueryScope.user, 'Diagnostics/Archive')]);

    const diagnostics = roots[0].children[0];

    it('should create the outer folder once', () => roots[0].children.length.should.equal(1));
    it('should hold both the nested folder and the query', () => diagnostics.children.length.should.equal(2));
    it('should order the folder ahead of the query', () => diagnostics.children[0].kind.should.equal(QueryNodeKind.Folder));
});

describe('when collecting the folders a query can be filed into', () => {
    const folders = foldersInScope(
        [
            queryWith('1', 'Failed appends', SequenceQueryScope.user, 'Diagnostics/Failures'),
            queryWith('2', 'Shared', SequenceQueryScope.everyone, 'Reporting')
        ],
        SequenceQueryScope.user,
        [folderAt(SequenceQueryScope.user, 'Archive')]);

    it('should include every level of a nested path', () =>
        folders.should.eql(['Archive', 'Diagnostics', 'Diagnostics/Failures']));
});
