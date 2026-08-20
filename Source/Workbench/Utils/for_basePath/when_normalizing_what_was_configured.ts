// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { normalizeBasePath } from '../basePath';

describe('when normalizing what was configured', () => {
    it('should treat an absent value as the root', () => normalizeBasePath(undefined).should.equal(''));
    it('should treat a null value as the root', () => normalizeBasePath(null).should.equal(''));
    it('should treat an empty value as the root', () => normalizeBasePath('').should.equal(''));
    it('should treat a lone slash as the root', () => normalizeBasePath('/').should.equal(''));
    it('should keep a single segment', () => normalizeBasePath('/workbench').should.equal('/workbench'));
    it('should keep every segment of a nested prefix', () => normalizeBasePath('/api/play/a-session/workbench').should.equal('/api/play/a-session/workbench'));
    it('should add a missing leading slash', () => normalizeBasePath('workbench').should.equal('/workbench'));
    it('should drop a trailing slash', () => normalizeBasePath('/workbench/').should.equal('/workbench'));
    it('should ignore surrounding whitespace', () => normalizeBasePath('  /workbench  ').should.equal('/workbench'));
});
