// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { classifyAttempt, collectReport, markdown, parseArgs } from './classify-reruns.mjs';

const now = Date.parse('2026-11-12T12:00:00Z');
const attempt = (conclusion, extra = {}) => ({ conclusion, head_sha: 'a'.repeat(40), triggering_actor: { login: 'person', type: 'User' }, ...extra });
const bot = { login: 'github-actions[bot]', type: 'Bot' };

test('gate refresh requires the known actor and workflow', () => {
    assert.equal(classifyAttempt('Hot Core Gate', attempt('failure'), attempt('success', { triggering_actor: bot })), 'gate-refresh');
    assert.equal(classifyAttempt('Requested Integration', attempt('failure'), attempt('success', { triggering_actor: bot })), 'flake');
});

test('probable flake requires failure then success on unchanged head', () => {
    assert.equal(classifyAttempt('Requested Integration', attempt('failure'), attempt('success')), 'flake');
    assert.equal(classifyAttempt('Requested Integration', attempt('failure'), attempt('success', { head_sha: 'b'.repeat(40) })), 'unclassified');
});

test('human deliberate reruns include green cancelled skipped or still red attempts', () => {
    for (const conclusion of ['success', 'cancelled', 'skipped']) {
        assert.equal(classifyAttempt('Hot Core Gate', attempt(conclusion), attempt('success')), 'deliberate');
    }
    assert.equal(classifyAttempt('Hot Core Gate', attempt('failure'), attempt('failure')), 'deliberate');
});

test('missing data and unknown actors or conclusions remain unclassified', () => {
    for (const missing of [null, {}, attempt(null), attempt('success', { triggering_actor: {} }), attempt('success', { head_sha: '' })]) {
        assert.equal(classifyAttempt('Hot Core Gate', attempt('failure'), missing), 'unclassified');
    }
    assert.equal(classifyAttempt('Hot Core Gate', null, attempt('success')), 'unclassified');
    assert.equal(classifyAttempt('Hot Core Gate', attempt('failure'), attempt('cancelled')), 'unclassified');
});

function fixture(runs, attempts = {}) {
    const reads = [];
    return { reads, fetcher: endpoint => {
        reads.push(endpoint);
        if (endpoint.includes('/workflows/')) return { workflow_runs: runs };
        if (!(endpoint in attempts)) throw new Error('attempt unavailable');
        return attempts[endpoint];
    } };
}

const run = (id, ageDays, extra = {}) => ({ id, created_at: new Date(now - ageDays * 86400000).toISOString(), run_attempt: 2, ...extra });
const endpoint = (id, number) => `repos/o/r/actions/runs/${id}/attempts/${number}`;

test('14 day window is inclusive and only reruns are fetched', async () => {
    const f = fixture([run(1, 14), run(2, 14.01), run(3, 1, { run_attempt: 1 })]);
    const report = await collectReport({ repo: 'o/r', now, fetcher: f.fetcher });
    assert.equal(report.attempts.length, 3, 'one missing-data attempt per workflow');
    assert.equal(report.counts.unclassified, 3);
    assert.equal(f.reads.filter(path => path.includes('/2/attempts/')).length, 0);
    assert.equal(f.reads.filter(path => path.includes('/3/attempts/')).length, 0);
});

test('every rerun attempt is classified and grouped by workflow', async () => {
    const f = fixture([run(4, 1, { run_attempt: 3 })], {
        [endpoint(4, 1)]: attempt('failure'), [endpoint(4, 2)]: attempt('success'), [endpoint(4, 3)]: attempt('success'),
    });
    const report = await collectReport({ repo: 'o/r', now, fetcher: f.fetcher });
    assert.equal(report.counts.flake, 3);
    assert.equal(report.counts.deliberate, 3);
    assert.match(markdown(report), /probable flake/);
    assert.match(markdown(report), /Hot Core Gate \| 0 \| 1 \| 1 \| 0/);
});

test('empty windows still print explicit zero counts', async () => {
    const report = await collectReport({ repo: 'o/r', now, fetcher: fixture([]).fetcher });
    assert.equal(report.attempts.length, 0);
    assert.deepEqual(report.counts, { 'gate-refresh': 0, flake: 0, deliberate: 0, unclassified: 0 });
    assert.match(markdown(report), /Total \| 0 \| 0 \| 0 \| 0/);
});

test('run list API failure cannot produce a false empty report', async () => {
    await assert.rejects(collectReport({ repo: 'o/r', now, fetcher: () => { throw new Error('rate limited'); } }), /rate limited/);
    await assert.rejects(collectReport({ repo: 'o/r', now, fetcher: () => ({}) }), /workflow_runs/);
});

test('pagination includes later pages', async () => {
    const reads = [];
    const fetcher = endpoint => {
        reads.push(endpoint);
        return { workflow_runs: endpoint.endsWith('page=1') ? Array.from({ length: 100 }, (_, i) => run(i + 1, 20)) : [] };
    };
    await collectReport({ repo: 'o/r', now, fetcher });
    assert.equal(reads.filter(path => path.endsWith('page=2')).length, 3);
});

test('CLI defaults and options validate inputs', () => {
    assert.deepEqual(parseArgs([]), { repo: 'Cratis/Chronicle', days: 14, json: false });
    assert.deepEqual(parseArgs(['--days', '7', '--repo', 'o/r', '--json']), { repo: 'o/r', days: 7, json: true });
    for (const args of [['--days', '0'], ['--days', 'x'], ['--repo', 'nope'], ['--oops']]) assert.throws(() => parseArgs(args));
});
