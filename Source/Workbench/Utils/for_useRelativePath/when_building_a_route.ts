// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { useRelativePath } from '../useRelativePath';

describe('when building a route', () => {
    it('should add a leading slash', () => useRelativePath('event-store').should.equal('/event-store'));
    it('should keep an existing leading slash single', () => useRelativePath('/event-store').should.equal('/event-store'));
    it('should leave the prefix to the router', () => useRelativePath('event-store').should.not.contain('my-instance'));
});
