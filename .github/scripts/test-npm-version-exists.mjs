// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { versionExists } from './npm-version-exists.mjs';

test('an exact version found on the registry exists', () => {
    assert.equal(versionExists({ status: 0, stdout: '"1.2.3"' }, '1.2.3'), true);
});

test('several versions of equal precedence are searched for the exact one', () => {
    assert.equal(versionExists({ status: 0, stdout: '["1.2.3","1.2.3+build"]' }, '1.2.3+build'), true);
});

test('E404 means the version is absent', () => {
    assert.equal(versionExists({ status: 1, stdout: '{"error":{"code":"E404"}}' }, '1.2.3'), false);
});

test('an empty successful answer means the version is absent', () => {
    assert.equal(versionExists({ status: 0, stdout: '' }, '1.2.3'), false);
});

for (const [description, lookup] of [
    ['a registry failure', { status: 1, stdout: '{"error":{"code":"E503"}}' }],
    ['invalid JSON', { status: 0, stdout: '<html>' }],
    ['a lookup that did not complete', { status: null, stdout: '' }],
    ['a lookup that could not start', { error: new Error('spawn'), status: 0, stdout: '' }],
    ['an unexpected response shape', { status: 0, stdout: '{"version":"1.2.3"}' }]
]) {
    test(`${description} fails instead of reporting a version`, () => {
        assert.throws(() => versionExists(lookup, '1.2.3'));
    });
}
