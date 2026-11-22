// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { downloadSpecArtifacts, expectedSpecArtifacts, reportInputError, selectSpecArtifacts } from './legacy-contract-artifacts.mjs';

const workflow = readFileSync(new URL('../workflows/dotnet-build.yml', import.meta.url), 'utf8');
const expected = expectedSpecArtifacts(workflow);
const artifact = (name, id, created_at = '2026-11-21T11:00:00Z') => ({ name, id, created_at, expired: false });
const fullRun = expected.map((name, index) => artifact(name, index + 1));
const pages = artifacts => Array.from({ length: Math.ceil(artifacts.length / 100) }, (_, index) => ({
    total_count: artifacts.length, artifacts: artifacts.slice(index * 100, (index + 1) * 100)
}));

function workspace(action) {
    const root = new URL('../../.ai-work/', import.meta.url);
    mkdirSync(root, { recursive: true });
    const directory = mkdtempSync(join(root.pathname, 'legacy-contract-artifacts-'));
    try {
        return action(directory);
    } finally {
        rmSync(directory, { recursive: true });
    }
}

function fakeApi(artifacts, calls, includeTrx = true) {
    return (command, args) => {
        calls.push({ command, args });
        if (args.includes('--paginate')) return JSON.stringify(pages(artifacts));
        if (command === 'unzip') {
            const destination = args.at(-1);
            mkdirSync(join(destination, 'project'), { recursive: true });
            if (includeTrx) writeFileSync(join(destination, 'project/results.trx'), '<TestRun/>');
        }
    };
}

function inputs(directory) {
    return { workflow, repository: 'Cratis/Chronicle', runId: '44900383278', directory };
}

test('expected inputs follow only the gate dependencies and their actual shard matrices', () => {
    assert.deepEqual(expected, [
        ...[1, 2, 3, 4].map(shard => `coverage-results-specs-other-${shard}`),
        ...['postgresql', 'sqlserver-1', 'sqlserver-2', 'sqlserver-3', 'sqlserver-4', 'remaining']
            .map(shard => `coverage-results-specs-sql-${shard}`)
    ].sort());
    const changed = workflow.replace('shard: [1, 2, 3, 4]', 'shard: [1, 2, 3, 4, 5]');
    assert.ok(expectedSpecArtifacts(changed).includes('coverage-results-specs-other-5'));
    const sqlChanged = workflow.replace('sqlserver-4, remaining]', 'sqlserver-4, sqlserver-5, remaining]');
    assert.ok(expectedSpecArtifacts(sqlChanged).includes('coverage-results-specs-sql-sqlserver-5'));
    const withoutSql = workflow.replace('grain-compatibility, specs-other, specs-sql]', 'grain-compatibility, specs-other]');
    assert.deepEqual(expectedSpecArtifacts(withoutSql), expected.filter(name => name.includes('-other-')));
});

test('unsupported or empty producer definitions fail closed instead of shrinking the expected set', () => {
    assert.throws(() => expectedSpecArtifacts(workflow.replace('shard: [1, 2, 3, 4]', 'include: [{shard: 1}]')), /unsupported matrix/);
    assert.throws(() => expectedSpecArtifacts(workflow.replace('coverage-results-specs-other-', 'renamed-results-')), /missing spec result upload in specs-other/);
    assert.throws(() => expectedSpecArtifacts(workflow.replace('grain-compatibility, specs-other, specs-sql]', 'grain-compatibility]')), /nonempty, unique artifact set/);
});

test('selection includes the final page beyond 100 artifacts and deduplicates across attempts', () => {
    const noise = Array.from({ length: 200 }, (_, index) => artifact(`integration-${index}`, 1000 + index));
    const newer = artifact(expected[0], 5000, '2026-11-21T12:00:00Z');
    const listed = pages([newer, ...noise, ...fullRun]);
    assert.equal(listed.length, 3);
    const selected = selectSpecArtifacts(listed, expected);
    assert.equal(selected.length, expected.length);
    assert.equal(selected[0].id, newer.id);
    assert.deepEqual(selected.slice(1), fullRun.slice(1));
    assert.deepEqual(selectSpecArtifacts(pages([...fullRun, newer]), expected), selected);
});

test('the newest timestamp wins regardless of listing order, with artifact ID breaking ties', () => {
    const name = expected[0];
    const old = artifact(name, 99);
    const newer = artifact(name, 10, '2026-11-21T12:00:00Z');
    const latest = { ...newer, id: 11 };
    assert.deepEqual(selectSpecArtifacts(pages([latest, old, newer]), [name]), [latest]);
    assert.deepEqual(selectSpecArtifacts(pages([old, newer, latest]), [name]), [latest]);
});

test('incomplete inputs name every missing artifact; an expired newest upload cannot reuse an older result', () => {
    assert.throws(() => selectSpecArtifacts(pages(fullRun.slice(2)), expected), {
        message: `Legacy contracts input incomplete: missing spec result artifacts: ${expected.slice(0, 2).join(', ')}`
    });
    const expired = { ...fullRun[0], id: 50, created_at: '2026-11-21T12:00:00Z', expired: true };
    assert.throws(() => selectSpecArtifacts(pages([...fullRun, expired]), expected), new RegExp(`input incomplete:.*${expected[0]}`));
    assert.throws(() => selectSpecArtifacts([], expected), /input incomplete: missing spec result artifacts/);
});

test('downloads use fully paginated REST listing and only the selected IDs in separate directories', () => workspace(directory => {
    const calls = [];
    const newer = artifact(expected[0], 50, '2026-11-21T12:00:00Z');
    const selected = downloadSpecArtifacts(inputs(directory), fakeApi([...fullRun, newer], calls));
    assert.deepEqual(calls[0], {
        command: 'gh', args: ['api', 'repos/Cratis/Chronicle/actions/runs/44900383278/artifacts?per_page=100', '--paginate', '--slurp']
    });
    assert.deepEqual(calls.filter(call => call.args.at(-1).endsWith('/zip')).map(call => call.args.at(-1)),
        selected.map(item => `repos/Cratis/Chronicle/actions/artifacts/${item.id}/zip`));
    assert.equal(selected[0].id, 50);
    assert.deepEqual(readdirSync(directory).sort(), expected);
    for (const name of expected) assert.equal(readFileSync(join(directory, name, 'project/results.trx'), 'utf8'), '<TestRun/>');
}));

test('missing artifacts produce an explicit error before any download or evaluation', () => workspace(directory => {
    const calls = [];
    let annotation;
    assert.throws(() => {
        try {
            downloadSpecArtifacts(inputs(directory), fakeApi(fullRun.slice(1), calls));
        } catch (error) {
            reportInputError(error, value => { annotation = value; });
            throw error;
        }
    }, /input incomplete/);
    assert.equal(calls.length, 1);
    assert.equal(annotation, `::error::Legacy contracts input incomplete: missing spec result artifacts: ${expected[0]}`);
    assert.deepEqual(readdirSync(directory), []);
}));

test('an artifact without TRX is explicitly incomplete, and a failed API call cannot become empty input', () => workspace(directory => {
    assert.throws(() => downloadSpecArtifacts(inputs(directory), fakeApi(fullRun, [], false)), /input incomplete: no TRX results in coverage-results-specs/);
    assert.throws(() => downloadSpecArtifacts(inputs(directory), () => { throw new Error('API unavailable'); }), /API unavailable/);
}));

test('both reusable and dispatched runs use the completeness-checking downloader before evaluation', () => {
    const legacy = readFileSync(new URL('../workflows/legacy-contracts.yml', import.meta.url), 'utf8');
    assert.match(legacy, /RUN_ID: \$\{\{ inputs\.run-id \|\| github\.run_id \}\}/);
    assert.match(legacy, /GH_TOKEN: \$\{\{ github\.token \}\}/);
    assert.match(legacy, /actions: read/);
    assert.doesNotMatch(legacy, /actions\/download-artifact|continue-on-error/);
    assert.ok(legacy.indexOf('run: node .github/scripts/legacy-contract-artifacts.mjs') < legacy.indexOf('- name: Evaluate the committed matrix'));
});
