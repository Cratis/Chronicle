// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { watchdog, main, marker } from './last-green-watchdog.mjs';
import { trackerTitle, workflowPath } from './nightly-build-health-policy.mjs';

globalThis.fetch = async () => { throw new Error('Live networking is forbidden in fixtures'); };
const repo = 'example/chronicle';
const now = Date.parse('2026-11-12T12:00:00Z');
const bot = { login: 'github-actions[bot]', type: 'Bot' };
const run = (id, age, conclusion = 'success', extra = {}) => ({ id, created_at: new Date(now - age * 3600000).toISOString(), conclusion, status: 'completed', event: 'schedule', path: workflowPath, head_branch: 'main', head_repository: { full_name: repo }, head_sha: 'a'.repeat(40), ...extra });

function fixture(runs) {
    const issues = [{ number: 5707, title: trackerTitle, user: bot, state: 'open' }];
    const comments = [];
    const writes = [];
    const transport = async (method, endpoint, body) => {
        if (method !== 'GET') {
            writes.push({ method, endpoint, body });
            if (method === 'POST') comments.push({ id: comments.length + 1, body: body.body, user: bot });
            else if (endpoint.includes('/comments/')) Object.assign(comments.find(c => endpoint.endsWith(`/${c.id}`)), body);
            else Object.assign(issues[0], body);
            return { data: {} };
        }
        if (endpoint.includes('/runs?')) {
            const event = new URL(`https://api.github.com${endpoint}`).searchParams.get('event');
            const matching = runs.filter(r => r.event === event);
            return { data: { total_count: matching.length, workflow_runs: matching } };
        }
        if (endpoint.includes('/jobs?')) return { data: { total_count: 2, jobs: [{ name: 'integration / Projections <script>', conclusion: 'failure' }, { name: 'already-ran', conclusion: 'success' }] } };
        if (endpoint.includes('/comments?')) return { data: structuredClone(comments) };
        assert.match(endpoint, /\/issues\?/);
        return { data: structuredClone(issues) };
    };
    return { issues, comments, writes, check: extra => watchdog({ repo, now, transport, ...extra }) };
}

test('recent dispatch success and skipped duplicate-run success count as green', async () => {
    for (const extra of [{ event: 'workflow_dispatch' }, {}]) {
        const f = fixture([run(1, 2, 'success', extra)]);
        assert.equal((await f.check()).verdict, 'ok');
        assert.equal(f.writes.length, 0);
    }
});

for (const runs of [[], [run(1, 37)]]) test(`stale creates exactly one bot comment (${runs.length} green runs)`, async () => {
    const f = fixture(runs);
    assert.equal((await f.check()).verdict, 'stale');
    assert.match(f.comments[0].body, /last-green-watchdog:v1/);
    await f.check({ now: now + 7200000 });
    assert.equal(f.writes.length, 1, 'age alone must not cause comment churn');
});

test('red latest, stale and red together, failing lanes and recovery', async () => {
    const runs = [run(1, 37), run(2, 1, 'failure')];
    const f = fixture(runs);
    assert.equal((await f.check()).verdict, 'stale+red');
    assert.match(f.comments[0].body, /Projections script/);
    runs.push(run(3, 0));
    assert.equal((await f.check()).verdict, 'ok');
    assert.match(f.comments[0].body, /recovered/);
    const red = fixture([run(1, 2), run(2, 1, 'failure')]);
    assert.equal((await red.check()).verdict, 'red');
});

test('closed tracker is reopened; human comment is never edited', async () => {
    const f = fixture([]);
    f.issues[0].state = 'closed';
    f.comments.push({ id: 90, user: { login: 'human' }, body: marker });
    await f.check();
    assert.equal(f.issues[0].state, 'open');
    assert.equal(f.comments[0].body, marker);
    assert.ok(f.writes.every(w => !w.endpoint.endsWith('/comments/90')));
});

test('missing or human-owned tracker fails safely', async () => {
    const f = fixture([]);
    f.issues[0].user = { login: 'human' };
    await assert.rejects(f.check(), /tracker-missing/);
    assert.equal(f.writes.length, 0);
});

test('foreign, PR, wrong-path and wrong-branch runs cannot establish green', async () => {
    const f = fixture([run(1, 1, 'success', { head_branch: 'other' }), run(2, 1, 'success', { path: 'other.yml' }), run(3, 1, 'success', { head_repository: { full_name: 'other/repo' } }), run(4, 1, 'success', { event: 'pull_request' })]);
    assert.equal((await f.check()).verdict, 'stale');
});

test('--dry-run entry point issues zero non-GET calls through injectable fetch', async () => {
    const methods = [];
    const fetchImpl = async (url, init) => {
        methods.push(init.method);
        let data;
        if (url.includes('/runs?')) data = { total_count: 0, workflow_runs: [] };
        else if (url.includes('/comments?')) data = [];
        else if (url.includes('/issues?')) data = [{ number: 5707, title: trackerTitle, user: bot, state: 'closed' }];
        else assert.fail(`Unexpected URL ${url}`);
        return { ok: true, json: async () => data, headers: new Headers() };
    };
    const output = [];
    assert.equal(await main({ environment: { GITHUB_REPOSITORY: repo, GITHUB_TOKEN: 'fixture' }, argv: ['--dry-run'], fetchImpl, now, output: line => output.push(line) }), 0);
    assert.ok(methods.length > 0);
    assert.ok(methods.every(method => method === 'GET'));
    assert.equal(JSON.parse(output[0]).verdict, 'stale');
});
