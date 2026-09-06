// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { absolutePath, basePath } from '../basePath';

describe('when building an absolute path', () => {
    // No base-path meta tag is present in the test document, so this exercises the root case - the one every
    // existing deployment is in, and the one a prefix must not change.
    it('should resolve to the root', () => basePath.should.equal(''));
    it('should keep a path that already leads with a slash', () => absolutePath('/api/event-stores').should.equal('/api/event-stores'));
    it('should add a leading slash to one that lacks it', () => absolutePath('api/event-stores').should.equal('/api/event-stores'));
    it('should keep the query string', () => absolutePath('/identity/login?useCookies=true').should.equal('/identity/login?useCookies=true'));
    it('should resolve the root path itself', () => absolutePath('/').should.equal('/'));
});
