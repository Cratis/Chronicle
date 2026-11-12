// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, rm, readFile } from 'node:fs/promises';
import path from 'node:path';
import { collect, render, record } from './failed-spec-record.mjs';
import { trackerTitle } from './nightly-build-health-policy.mjs';

globalThis.fetch = async () => { throw new Error('Live networking is forbidden in fixtures'); };

async function tree(t) {
    await mkdir('.ai-work/r1', { recursive: true });
    const root = await mkdtemp(path.resolve('.ai-work/r1/failed-spec-test-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    for (const [file, xml] of [
        ['coverage-results-Projections/attempt-1/results.trx', '<UnitTestResult testName="First&lt;bad&gt;" outcome="Failed"/><UnitTestResult outcome="Passed" testName="Passed"/>'],
        ['coverage-results-Projections/attempt-2/results.trx', '<UnitTestResult outcome="Failed" testName="Last"/>'],
        ['sql-clustering-SqlServer-results/coverage-results/sql.trx', '<UnitTestResult testName="Sql.Spec" outcome="Failed"/>']
    ]) {
        await mkdir(path.dirname(path.join(root, file)), { recursive: true });
        await writeFile(path.join(root, file), xml);
    }
    await mkdir(path.join(root, 'coverage-results-Crashed'));
    return root;
}

test('first failure, last attempt, SQL and missing TRX are retained by lane', async t => {
    const lanes = await collect(await tree(t));
    const body = render(lanes, { repo: 'example/chronicle', runId: 12, attempt: 1 });
    assert.match(body, /failed on attempt 1:.*Firstbad/s);
    assert.match(body, /still failing after the last attempt:.*Last/s);
    assert.match(body, /### artifact sql-clustering-SqlServer-results\n\nfailed on attempt 1:\n- Sql\.Spec/);
    assert.match(body, /still failing after the last attempt:\n- Sql\.Spec/);
    assert.match(body, /no TRX found for coverage-results-Crashed/);
    assert.doesNotMatch(body, /Passed/);
});

test('per-lane and total caps leave artifact pointers and omitted counts', () => {
    const names = Array.from({ length: 450 }, (_, i) => `Spec.${i}.${'long'.repeat(50)}`);
    const lanes = Array.from({ length: 20 }, (_, i) => ({ name: `coverage-results-${i}`, attempts: new Map([[1, names], [2, names]]), files: 1 }));
    const body = render(lanes, { repo: 'example/chronicle', runId: 12, attempt: 1 });
    assert.ok(body.length < 65536);
    assert.match(body, /\+\d+ more/);
    assert.match(body, /artifacts/);
    const artifact = render(lanes, { repo: 'example/chronicle', runId: 12, attempt: 1 }, { full: true });
    assert.match(artifact, /Spec\.449\./);
    assert.match(artifact, /coverage-results-19/);
    assert.doesNotMatch(artifact, /\+\d+ more/);
});

test('one bot comment per run attempt; human markers cannot suppress it; Markdown file is written', async t => {
    const root = await tree(t);
    const out = path.join(root, 'failed-specs.md');
    const comments = [{ id: 1, user: { login: 'human' }, body: '<!-- failed-specs:run-12-attempt-1 -->' }];
    const writes = [];
    const transport = async (method, endpoint, body) => {
        if (method !== 'GET') {
            writes.push({ method, endpoint, body });
            comments.push({ id: 2, user: { login: 'github-actions[bot]', type: 'Bot' }, body: body.body });
            return { data: {} };
        }
        if (endpoint.includes('/comments?')) return { data: comments };
        assert.match(endpoint, /\/issues\?/);
        return { data: [{ number: 5707, title: trackerTitle, user: { login: 'github-actions[bot]', type: 'Bot' } }] };
    };
    const options = { root, out, repo: 'example/chronicle', runId: 12, attempt: 1, transport };
    await record(options);
    await record(options);
    assert.equal(writes.length, 1);
    assert.match(await readFile(out, 'utf8'), /failed-specs:run-12-attempt-1/);
});

test('empty download is explicitly recorded rather than silently succeeding', async t => {
    const root = await tree(t);
    const empty = path.join(root, 'empty');
    await mkdir(empty);
    assert.match(render(await collect(empty), { repo: 'example/chronicle', runId: 12, attempt: 1 }), /no TRX found/);
});
