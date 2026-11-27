// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { inspectPackedFiles, requiredFiles } from './inspect-pack.mjs';

test('a complete artifact has no problems', () => {
    assert.deepEqual(inspectPackedFiles(requiredFiles), []);
});

test('a missing dist file is reported', () => {
    const problems = inspectPackedFiles(requiredFiles.filter(file => file !== 'dist/styles.css'));
    assert.deepEqual(problems, ['missing dist/styles.css']);
});

test('declarations of the staged generated types are packed', () => {
    assert.deepEqual(inspectPackedFiles([...requiredFiles, 'dist/types/generated-input/EventStores/EventStoreNames.d.ts']), []);
});

for (const extra of ['.env', 'dist/index.js.map', 'wwwroot/index.html', 'generated-input/EventStores/ObserveEventStores.ts', 'dist/monaco-worker.js']) {
    test(`${extra} must not be packed`, () => {
        assert.notDeepEqual(inspectPackedFiles([...requiredFiles, extra]), []);
    });
}
