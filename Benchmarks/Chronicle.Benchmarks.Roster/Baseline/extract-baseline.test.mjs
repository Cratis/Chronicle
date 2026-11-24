// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { copyFileSync, mkdirSync, mkdtempSync, readFileSync, renameSync, statSync, utimesSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const sourceDirectory = dirname(fileURLToPath(import.meta.url));
const repository = resolve(sourceDirectory, '../../..');
const revision = 'a5e06dbf0e';
const filenames = ['AdministratorRoster.cs', 'AdministratorRosterLogging.cs'];
const fixtureRoot = join(repository, '.ai-work', 'baseline-extraction-tests');
mkdirSync(fixtureRoot, { recursive: true });

function fixture() {
    const directory = mkdtempSync(join(fixtureRoot, 'case-'));
    for (const name of ['extract-baseline.mjs', 'Baseline.csproj']) {
        copyFileSync(join(sourceDirectory, name), join(directory, name));
    }
    const output = join(directory, 'obj');
    const run = (requestedRevision = revision, cwd = repository) => spawnSync(process.execPath,
        [join(directory, 'extract-baseline.mjs'), requestedRevision, output], { cwd, encoding: 'utf8' });
    return { directory, output, run, stamp: join(output, 'baseline-extraction.json') };
}

function succeeds(result) {
    assert.equal(result.status, 0, result.stderr);
}

function snapshot(paths) {
    return paths.map(path => ({ contents: readFileSync(path), mtime: statSync(path, { bigint: true }).mtimeNs }));
}

function age(paths) {
    for (const path of paths) utimesSync(path, 1, 1);
}

test('a clean extraction matches the pinned Git sources', () => {
    const current = fixture();
    succeeds(current.run());
    for (const name of filenames) {
        const expected = execFileSync('git', ['show', `${revision}:Source/Kernel/Core/Security/${name}`], { cwd: repository });
        assert.deepEqual(readFileSync(join(current.output, name)), expected);
    }
    assert.equal(JSON.parse(readFileSync(current.stamp)).revision, revision);
});

test('an unchanged extraction preserves bytes and timestamps without invoking Git', () => {
    const current = fixture();
    succeeds(current.run());
    const paths = [...filenames.map(name => join(current.output, name)), current.stamp];
    age(paths);
    const before = snapshot(paths);
    // Outside a Git repository, any redundant extraction would fail.
    succeeds(current.run(revision, '/'));
    assert.deepEqual(snapshot(paths), before);
});

test('a changed revision regenerates the sources', () => {
    const current = fixture();
    succeeds(current.run());
    const changedRevision = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: repository, encoding: 'utf8' }).trim();
    succeeds(current.run(changedRevision));
    for (const name of filenames) {
        const expected = execFileSync('git', ['show', `${changedRevision}:Source/Kernel/Core/Security/${name}`], { cwd: repository });
        assert.deepEqual(readFileSync(join(current.output, name)), expected);
    }
    assert.equal(JSON.parse(readFileSync(current.stamp)).revision, changedRevision);
});

for (const missing of filenames) {
    test(`a missing ${missing} is restored without rewriting the other source`, () => {
        const current = fixture();
        succeeds(current.run());
        const paths = filenames.map(name => join(current.output, name));
        age(paths);
        const before = snapshot(paths);
        renameSync(join(current.output, missing), join(current.directory, missing));
        succeeds(current.run());
        assert.deepEqual(readFileSync(join(current.output, missing)), readFileSync(join(current.directory, missing)));
        const other = filenames.findIndex(name => name !== missing);
        assert.deepEqual(snapshot([paths[other]]), [before[other]]);
    });
}

for (const file of ['Baseline.csproj', 'extract-baseline.mjs']) {
    test(`a changed ${file} invalidates the cache`, () => {
        const current = fixture();
        succeeds(current.run());
        const previous = readFileSync(current.stamp);
        const source = join(current.output, filenames[0]);
        const expected = readFileSync(source);
        writeFileSync(source, 'stale source');
        const path = join(current.directory, file);
        const comment = file.endsWith('.mjs') ? '\n// Changed extraction implementation.\n' : '\n<!-- Changed project. -->\n';
        writeFileSync(path, readFileSync(path, 'utf8') + comment);
        succeeds(current.run());
        assert.notDeepEqual(readFileSync(current.stamp), previous);
        assert.deepEqual(readFileSync(source), expected);
    });
}

test('an invalid revision fails without truncating last-good sources or publishing success', () => {
    const current = fixture();
    succeeds(current.run());
    const paths = filenames.map(name => join(current.output, name));
    age(paths);
    const before = snapshot(paths);
    assert.notEqual(current.run('missing-baseline-revision').status, 0);
    assert.deepEqual(snapshot(paths), before);
    assert.equal(readFileSync(current.stamp).length, 0);
    succeeds(current.run());
    assert.deepEqual(snapshot(paths), before);
});

test('failure extracting the second source does not replace the first source', () => {
    const current = fixture();
    succeeds(current.run());
    const paths = filenames.map(name => join(current.output, name));
    age(paths);
    const before = snapshot(paths);
    const helper = join(current.directory, 'extract-baseline.mjs');
    writeFileSync(helper, readFileSync(helper, 'utf8').replace('Security/AdministratorRosterLogging.cs', 'Security/MissingBaseline.cs'));
    assert.notEqual(current.run().status, 0);
    assert.deepEqual(snapshot(paths), before);
    assert.equal(readFileSync(current.stamp).length, 0);
});

test('a Git failure leaves no successful extraction stamp', () => {
    const current = fixture();
    assert.notEqual(current.run(revision, '/').status, 0);
    assert.equal(readFileSync(current.stamp).length, 0);
});
