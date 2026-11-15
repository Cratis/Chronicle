// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { createSequenceQueryState } from '../../QueryEditor/SequenceQueryState';
import { applyChanges, followRenamedFolder } from '../folderChanges';

const refusedIds = new Set(['theirs']);
const accepts = (id: string) => Promise.resolve(!refusedIds.has(id));

/**
 * Deleting a folder closes the tabs of the queries that went with it, and only those - a query the
 * event store refused to delete is still there, and so are the edits open against it.
 */
describe('when deleting a folder that holds a query of another owner', () => {
    let outcome: Awaited<ReturnType<typeof applyChanges<string>>>;

    beforeEach(async () => {
        outcome = await applyChanges(['mine', 'theirs', 'also-mine'], accepts);
    });

    it('should report the deleted queries so their tabs close', () => outcome.accepted.should.eql(['mine', 'also-mine']));
    it('should report the refused query so its tab stays open', () => outcome.refused.should.eql(['theirs']));
});

describe('when nothing is refused', () => {
    it('should accept everything and refuse nothing', async () => {
        const outcome = await applyChanges(['mine', 'also-mine'], accepts);
        outcome.accepted.should.eql(['mine', 'also-mine']);
        outcome.refused.length.should.equal(0);
    });
});

/**
 * Open tabs remember the folder they were last saved in, so a rename only moves the ones the event
 * store accepted.
 */
describe('when renaming a folder that holds a query of another owner', () => {
    const open = (id: string, scope = SequenceQueryScope.everyone) =>
        createSequenceQueryState(id, id, 'default', scope, 'Diagnostics/Failures');
    const rename = (state: ReturnType<typeof open>) =>
        followRenamedFolder(state, SequenceQueryScope.everyone, 'Diagnostics', 'Operations', refusedIds);

    it('should move the saved baseline of a query that was accepted', () =>
        rename(open('mine')).folder.should.equal('Operations/Failures'));

    it('should leave the baseline of the refused query where it was', () =>
        rename(open('theirs')).folder.should.equal('Diagnostics/Failures'));

    it('should leave queries in another scope alone', () =>
        rename(open('mine', SequenceQueryScope.user)).folder.should.equal('Diagnostics/Failures'));
});
