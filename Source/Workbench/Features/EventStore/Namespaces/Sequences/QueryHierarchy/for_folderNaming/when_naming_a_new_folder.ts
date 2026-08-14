// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renamedFolderPath, rewriteFolderPath, uniqueFolderPath } from '../folderNaming';

describe('when the name is not taken', () => {
    it('should use it as it stands', () => uniqueFolderPath('', 'New folder', []).should.equal('New folder'));
});

describe('when the name is already taken', () => {
    it('should number the new one', () =>
        uniqueFolderPath('', 'New folder', ['New folder']).should.equal('New folder 2'));
});

describe('when several numbered names are already taken', () => {
    it('should carry on past them', () =>
        uniqueFolderPath('', 'New folder', ['New folder', 'New folder 2', 'New folder 3']).should.equal('New folder 4'));
});

describe('when the folder goes underneath another one', () => {
    it('should build the full path', () =>
        uniqueFolderPath('Diagnostics', 'New folder', []).should.equal('Diagnostics/New folder'));

    it('should only consider names taken within that parent', () =>
        uniqueFolderPath('Diagnostics', 'New folder', ['New folder']).should.equal('Diagnostics/New folder'));
});

describe('when renaming a nested folder', () => {
    it('should replace only its own segment', () =>
        renamedFolderPath('Diagnostics/Failures', 'Errors').should.equal('Diagnostics/Errors'));

    it('should replace the whole path for a folder at the root', () =>
        renamedFolderPath('Diagnostics', 'Operations').should.equal('Operations'));
});

/**
 * A folder is the path its queries carry, so renaming one has to move everything filed at or
 * underneath it - and nothing else.
 */
describe('when rewriting paths after a folder moved', () => {
    it('should move the folder itself', () =>
        rewriteFolderPath('Diagnostics', 'Diagnostics', 'Operations').should.equal('Operations'));

    it('should move what is filed underneath it', () =>
        rewriteFolderPath('Diagnostics/Failures', 'Diagnostics', 'Operations').should.equal('Operations/Failures'));

    it('should leave a sibling alone', () =>
        rewriteFolderPath('Diagnostics2', 'Diagnostics', 'Operations').should.equal('Diagnostics2'));

    it('should leave an unrelated folder alone', () =>
        rewriteFolderPath('Reporting', 'Diagnostics', 'Operations').should.equal('Reporting'));
});
